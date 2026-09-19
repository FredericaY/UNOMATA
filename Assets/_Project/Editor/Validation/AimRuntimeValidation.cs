using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Unomata.Editor.Validation
{
    public static class AimRuntimeValidation
    {
        private static AimRuntimeInputDriver _driver;
        public static string Start(string mode="smoke",int fps=60,bool record=false,bool resume=false)
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            if(_driver!=null && _driver.Result!=null && _driver.Result.state=="running")
                throw new InvalidOperationException("A validation run is already active.");
            Detach();
            if(_driver!=null && _driver.Fixture!=null)UnityEngine.Object.Destroy(_driver.Fixture);
            _driver=new AimRuntimeInputDriver(new GameObject("AimRuntimeValidation_Fixture"));
            try{_driver.Configure(mode,fps,record,resume);}
            catch(Exception error){_driver.AbortSetup(error);throw;}
            var loop=PlayerLoop.GetCurrentPlayerLoop();
            Insert(ref loop,typeof(UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate),
                new PlayerLoopSystem{type=typeof(AimRuntimeInputDriver),updateDelegate=_driver.Tick},false);
            Insert(ref loop,typeof(UnityEngine.PlayerLoop.PreLateUpdate.ScriptRunBehaviourLateUpdate),
                new PlayerLoopSystem{type=typeof(AimRuntimeSampleDriver),updateDelegate=_driver.Sample},true);
            PlayerLoop.SetPlayerLoop(loop);
            AssemblyReloadEvents.beforeAssemblyReload+=OnReload;
            EditorApplication.playModeStateChanged+=OnPlayMode;
            return Status();
        }
        public static string Status()
        {
            if(_driver==null)return "{\"state\":\"idle\"}";
            var r=_driver.Result;
            return JsonConvert.SerializeObject(new{r.state,r.mode,r.currentCase,r.targetFps,r.completed,r.total,r.error,r.cleanupComplete});
        }
        public static string Cancel(){if(_driver!=null)_driver.Cancel();Detach();return Status();}
        private static void OnReload(){Cancel();}
        private static void OnPlayMode(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)Cancel();}
        internal static void Detach()
        {
            AssemblyReloadEvents.beforeAssemblyReload-=OnReload;
            EditorApplication.playModeStateChanged-=OnPlayMode;
            var loop=PlayerLoop.GetCurrentPlayerLoop();
            Remove(ref loop);
            PlayerLoop.SetPlayerLoop(loop);
        }
        private static bool Insert(ref PlayerLoopSystem system,Type target,PlayerLoopSystem value,bool after)
        {
            if(system.subSystemList==null)return false;
            var list=new List<PlayerLoopSystem>(system.subSystemList);
            for(int i=0;i<list.Count;i++)
            {
                if(list[i].type==target)
                {
                    list.Insert(i+(after?1:0),value);
                    system.subSystemList=list.ToArray();return true;
                }
                var child=list[i];
                if(Insert(ref child,target,value,after))
                {list[i]=child;system.subSystemList=list.ToArray();return true;}
            }
            return false;
        }
        private static void Remove(ref PlayerLoopSystem system)
        {
            if(system.subSystemList==null)return;
            var list=new List<PlayerLoopSystem>();
            foreach(var item in system.subSystemList)
            {
                if(item.type==typeof(AimRuntimeInputDriver)||item.type==typeof(AimRuntimeSampleDriver))continue;
                var child=item;Remove(ref child);list.Add(child);
            }
            system.subSystemList=list.ToArray();
        }
    }
}