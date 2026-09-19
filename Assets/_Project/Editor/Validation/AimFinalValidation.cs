using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
namespace Unomata.Editor.Validation
{
    public static class AimFinalValidation
    {
        private sealed class Stage { public string Name;public Action Start,Cancel;public Func<string> Status; }
        private static Stage _active;private static bool _running,_includeMatrix,_resumeMatrix;private static string _state="idle",_error;
        private static readonly List<object> Results=new List<object>();
        public static string Status()=>JsonConvert.SerializeObject(new{state=_state,phase=_active?.Name,error=_error,completed=Results.Count});
        public static string Start(bool includeMatrix=false,bool resumeMatrix=false)
        {
            if(!EditorApplication.isPlaying||_running)throw new InvalidOperationException("Requires idle Play Mode after the full matrix.");
            _includeMatrix=includeMatrix;_resumeMatrix=resumeMatrix;_running=true;_state="running";_error=null;Results.Clear();Run();return Status();
        }
        public static void Cancel(){_running=false;_active?.Cancel?.Invoke();}
        private static async void Run()
        {
            try
            {
                var stages=new List<Stage>{
                    new Stage{Name="Feedback recording",Start=()=>AimRuntimeValidation.Start("feedback",60,true),Cancel=()=>AimRuntimeValidation.Cancel(),Status=AimRuntimeValidation.Status},
                    new Stage{Name="120fps actions",Start=()=>AimRuntimeValidation.Start("categories",120,false),Cancel=()=>AimRuntimeValidation.Cancel(),Status=AimRuntimeValidation.Status},
                    new Stage{Name="30fps actions",Start=()=>AimRuntimeValidation.Start("categories",30,false),Cancel=()=>AimRuntimeValidation.Cancel(),Status=AimRuntimeValidation.Status},
                    new Stage{Name="120fps boundaries",Start=()=>AimBoundaryValidation.Start(120),Cancel=AimBoundaryValidation.Cancel,Status=AimBoundaryValidation.Status},
                    new Stage{Name="30fps boundaries",Start=()=>AimBoundaryValidation.Start(30),Cancel=AimBoundaryValidation.Cancel,Status=AimBoundaryValidation.Status},
                    new Stage{Name="Live input and cloth",Start=()=>AimInputSceneValidation.Start(),Cancel=AimInputSceneValidation.Cancel,Status=AimInputSceneValidation.Status}
                };
                if(_includeMatrix)stages.Insert(0,new Stage{Name="60fps full matrix",Start=()=>AimRuntimeValidation.Start("matrix",60,false,_resumeMatrix),Cancel=()=>AimRuntimeValidation.Cancel(),Status=AimRuntimeValidation.Status});
                foreach(var stage in stages)
                {
                    if(!_running||!EditorApplication.isPlaying)throw new OperationCanceledException();
                    _active=stage;Save();stage.Start();
                    JObject result;
                    do{await Task.Delay(125);result=JObject.Parse(stage.Status());}while((string)result["state"]=="running"&&EditorApplication.isPlaying);
                    Results.Add(new{name=stage.Name,result});Save();
                    if((string)result["state"]!="passed")throw new InvalidOperationException(stage.Name+": "+result);
                }
                _state="passed";
            }
            catch(Exception ex){_state=_running?"failed":"cancelled";_error=ex.ToString();_active?.Cancel?.Invoke();}
            finally{_running=false;Save();}
        }
        private static void Save(){Directory.CreateDirectory(".utmp/aim-repair");File.WriteAllText(".utmp/aim-repair/final-validation.json",JsonConvert.SerializeObject(new{state=_state,phase=_active?.Name,error=_error,completed=Results.Count,stages=Results},Formatting.Indented));}
    }
}