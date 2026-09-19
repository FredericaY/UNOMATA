using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using QFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unomata.Gameplay;
using Object = UnityEngine.Object;

namespace Unomata.Editor.Validation
{
    public static class AimGeometryValidation
    {
        public static string Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Run isolated geometry tests in Edit Mode.");
            var passed = new List<string>();
            Action<bool,string> check = (ok,label) => { if (!ok) throw new InvalidOperationException(label); passed.Add(label); };
            var random = new System.Random(1937);
            float maxError = 0;
            for (int i=0;i<256;i++)
            {
                var pivot = new Vector3(0.2f, 1.3f, 0.3f);
                var muzzle = new Vector3(0.12f,-0.04f,0.65f);
                var localRotation = Quaternion.Euler((float)random.NextDouble()*50-25, (float)random.NextDouble()*360, 0);
                float distance = new[]{3f,10f,50f,200f}[i%4];
                var direction = Quaternion.Euler((float)random.NextDouble()*100-30,(float)random.NextDouble()*360,0)*Vector3.forward;
                var target = pivot+direction*distance;
                check(AimGeometry.TrySolve(pivot,target,muzzle,localRotation,Vector3.up,0.05f,out var rotation),"solve "+i);
                var actualMuzzle = pivot+rotation*muzzle;
                float error = AimGeometry.AngleDegrees(rotation*localRotation*Vector3.forward,target-actualMuzzle);
                maxError=Mathf.Max(maxError,error);
                check(error<0.001f,"actual muzzle intersection "+i);
            }
            check(!AimGeometry.TrySolve(Vector3.zero,new Vector3(0,0,0.01f),new Vector3(1,0,0),Quaternion.identity,Vector3.up,0.05f,out _),"unreachable lateral offset");
            check(!AimGeometry.TrySolve(Vector3.zero,Vector3.zero,Vector3.zero,Quaternion.identity,Vector3.up,0.05f,out _),"coincident target");
            check(!AimGeometry.TrySolve(Vector3.zero,Vector3.forward*10,Vector3.zero,new Quaternion(0,0,0,0),Vector3.up,0.05f,out _),"invalid local quaternion");
            check(!AimGeometry.TrySolve(Vector3.zero,new Vector3(float.NaN,0,0),Vector3.zero,Quaternion.identity,Vector3.up,0.05f,out _),"non finite target");

            var app=AimGeometryTestArchitecture.Interface;
            try
            {
                var world=(AimTestWorld)app.GetUtility<IAimWorldQuery>();
                var context=Guid.NewGuid();
                app.SendCommand(new BeginAimContextCommand(context,new[]{9}));
                check(world.IgnoredCount==1,"context collider filter");
                check(app.SendQuery(new AimSnapshotQuery(context,1)).Status==AimStatus.Invalid,"unprepared snapshot unavailable");
                Func<int,bool,bool,AimFrameInput> input=(frame,aiming,transition)=>new AimFrameInput(context,frame,aiming,transition,
                    new Vector3(0,1.5f,-2),Vector3.forward,Vector3.up,new Vector3(0.2f,1.3f,0.3f),
                    new Vector3(0.05f,-0.02f,0.65f),Quaternion.identity,200,-1,-1);
                world.Reset();
                app.SendCommand(new PrepareAimFrameCommand(input(2,true,false)));
                var pose=app.SendQuery(new AimPoseQuery(context,2));
                check(pose.IsValid && Mathf.Abs(pose.Target.z-198)<0.001f,"far point fallback");
                check(app.SendQuery(new AimSnapshotQuery(context,2)).Status==AimStatus.Invalid,"prepare cannot publish ready");
                app.SendCommand(new CompleteAimFrameCommand(context,2,pose.PivotPosition+pose.PivotRotation*input(2,true,false).MuzzleLocalPosition,pose.PivotRotation*Vector3.forward));
                var snapshot=app.SendQuery(new AimSnapshotQuery(context,2));
                check(snapshot.Status==AimStatus.Ready && snapshot.AimErrorDegrees<0.001f,"final measured pose published");
                int calls=world.Calls;
                for(int i=0;i<5;i++) app.SendQuery(new AimSnapshotQuery(context,2));
                check(world.Calls==calls,"queries have no physics side effects");
                check(app.SendQuery(new AimSnapshotQuery(context,3)).Status==AimStatus.Invalid,"old frame rejected");
                world.Reset(); world.CameraTarget=new Vector3(0,1.5f,8);
                app.SendCommand(new PrepareAimFrameCommand(input(3,true,true)));
                pose=app.SendQuery(new AimPoseQuery(context,3));
                app.SendCommand(new CompleteAimFrameCommand(context,3,pose.PivotPosition+pose.PivotRotation*input(3,true,true).MuzzleLocalPosition,pose.PivotRotation*Vector3.forward));
                check(app.SendQuery(new AimSnapshotQuery(context,3)).Status==AimStatus.Transition,"transition not ready");
                app.SendCommand(new CompleteAimFrameCommand(context,3,pose.PivotPosition+pose.PivotRotation*input(3,true,true).MuzzleLocalPosition,-(pose.PivotRotation*Vector3.forward)));
                check(app.SendQuery(new AimSnapshotQuery(context,3)).Status==AimStatus.Transition,"opposite barrel during raising remains transition");
                world.Reset(); world.CameraInside=true;
                app.SendCommand(new PrepareAimFrameCommand(input(4,true,false)));
                check(app.SendQuery(new AimSnapshotQuery(context,4)).Failure==AimFailure.CameraInsideObstacle,"camera inside obstacle");
                world.Reset(); world.MuzzleInside=true;
                app.SendCommand(new PrepareAimFrameCommand(input(5,true,false)));
                pose=app.SendQuery(new AimPoseQuery(context,5));
                app.SendCommand(new CompleteAimFrameCommand(context,5,pose.PivotPosition+pose.PivotRotation*input(5,true,false).MuzzleLocalPosition,pose.PivotRotation*Vector3.forward));
                check(app.SendQuery(new AimSnapshotQuery(context,5)).Failure==AimFailure.MuzzleInsideObstacle,"muzzle inside obstacle");
                world.Reset(); world.Obstruction=new Vector3(0,1.5f,2);
                app.SendCommand(new PrepareAimFrameCommand(input(6,true,false)));
                pose=app.SendQuery(new AimPoseQuery(context,6));
                app.SendCommand(new CompleteAimFrameCommand(context,6,pose.PivotPosition+pose.PivotRotation*input(6,true,false).MuzzleLocalPosition,pose.PivotRotation*Vector3.forward));
                snapshot=app.SendQuery(new AimSnapshotQuery(context,6));
                check(snapshot.Status==AimStatus.Blocked && snapshot.HasObstruction && snapshot.DesiredTarget.z>snapshot.ObstructionPoint.z,"occlusion distinct from target");
                world.Reset(); world.CameraTarget=new Vector3(0,1.5f,-0.5f);
                app.SendCommand(new PrepareAimFrameCommand(input(7,true,false)));
                check(app.SendQuery(new AimSnapshotQuery(context,7)).Failure==AimFailure.TargetUnreachable,"target behind weapon");
                world.Reset();
                app.SendCommand(new PrepareAimFrameCommand(input(8,false,false)));
                check(app.SendQuery(new AimSnapshotQuery(context,8)).Status==AimStatus.Inactive,"non aim state");
                app.SendCommand(new InvalidateAimCommand(context,9,true));
                check(app.GetModel<AimModel>().ActiveContext==Guid.Empty && world.IgnoredCount==0,"release clears context and filter");
                var next=Guid.NewGuid();
                app.SendCommand(new BeginAimContextCommand(next,Array.Empty<int>()));
                app.SendCommand(new PrepareAimFrameCommand(input(10,true,false)));
                check(!app.SendQuery(new AimPoseQuery(context,10)).IsValid,"old scene cannot prepare");
                app.SendCommand(new InvalidateAimCommand(context,10,true));
                check(app.GetModel<AimModel>().ActiveContext==next,"old scene cannot clear new scene");
                context=next;
                for(int fault=0;fault<7;fault++)
                {
                    int frame=20+fault;
                    var bad=new AimFrameInput(context,frame,true,false,new Vector3(0,1.5f,-2),Vector3.forward,
                        fault==5?Vector3.zero:Vector3.up,new Vector3(0.2f,1.3f,0.3f),new Vector3(0.05f,0,0.65f),
                        fault==6?new Quaternion():Quaternion.identity,fault==0?float.NaN:fault==1?0:25,
                        fault==2?0:-1,fault==3?0:-1,fault==4?-1:0.05f);
                    app.SendCommand(new PrepareAimFrameCommand(bad));
                    check(app.SendQuery(new AimSnapshotQuery(context,frame)).Failure==AimFailure.MissingConfiguration,"invalid frame configuration "+fault);
                }
                world.Reset();
                var recovery=new AimFrameInput(context,40,true,false,new Vector3(0,1.5f,-2),Vector3.forward,Vector3.up,
                    new Vector3(0.2f,1.3f,0.3f),new Vector3(0.05f,0,0.65f),Quaternion.identity,25,-1,-1);
                app.SendCommand(new PrepareAimFrameCommand(recovery));
                pose=app.SendQuery(new AimPoseQuery(context,40));
                check(pose.IsValid&&Mathf.Abs(pose.Target.z-23)<0.001f,"repaired configuration and custom far distance");
                app.SendCommand(new CompleteAimFrameCommand(context,40,pose.PivotPosition+pose.PivotRotation*recovery.MuzzleLocalPosition,pose.PivotRotation*Vector3.forward));
                check(app.SendQuery(new AimSnapshotQuery(context,40)).Status==AimStatus.Ready,"recovery ready");
                app.SendCommand(new PrepareAimFrameCommand(input(39,true,false)));
                check(app.SendQuery(new AimSnapshotQuery(context,40)).Status==AimStatus.Ready,"late prepare cannot replace newer frame");
                app.SendCommand(new CompleteAimFrameCommand(context,39,Vector3.zero,Vector3.forward));
                check(app.SendQuery(new AimSnapshotQuery(context,40)).Status==AimStatus.Ready,"late completion cannot replace newer frame");
                app.SendCommand(new CompleteAimFrameCommand(context,40,pose.PivotPosition+pose.PivotRotation*recovery.MuzzleLocalPosition,-(pose.PivotRotation*Vector3.forward)));
                check(app.SendQuery(new AimSnapshotQuery(context,40)).Failure==AimFailure.TargetUnreachable,"opposite barrel outside transition remains invalid");
                app.SendCommand(new CompleteAimFrameCommand(context,40,pose.PivotPosition+pose.PivotRotation*recovery.MuzzleLocalPosition,pose.PivotRotation*Vector3.forward,false));
                check(app.SendQuery(new AimSnapshotQuery(context,40)).Failure==AimFailure.InvalidPose,"unreachable final grips cannot publish ready");

            }
            finally { app.Deinit(); }

            float maxPoleDrift=0;int legacyPoleFailures=0;
            for(int i=0;i<256;i++)
            {
                var axis=new Vector3((float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f).normalized;
                var from=Vector3.Cross(axis,Mathf.Abs(axis.y)<.9f?Vector3.up:Vector3.right).normalized;
                foreach(float angle in new[]{0f,90f,179.999f,180f,-179.999f,-180f})
                {
                    var to=Quaternion.AngleAxis(angle,axis)*from;
                    var rotation=StableArmIKJob.PoleRotation(axis,from,to,1);
                    float drift=(rotation*(axis*.48f)-axis*.48f).magnitude;
                    maxPoleDrift=Mathf.Max(maxPoleDrift,drift);
                    check(drift<0.00001f,"pole preserves solved endpoint "+i+"/"+angle);
                    check(AimGeometry.AngleDegrees(rotation*from,to)<0.001f,"pole reaches requested plane "+i+"/"+angle);
                    var legacy=UnityEngine.Animations.Rigging.QuaternionExt.FromToRotation(from,to);
                    if((legacy*(axis*.48f)-axis*.48f).magnitude>.01f)legacyPoleFailures++;
                }
            }
            check(legacyPoleFailures>0,"regression reproduces old opposite-pole endpoint drift");
            Directory.CreateDirectory(".utmp/aim-repair");
            File.WriteAllText(".utmp/aim-repair/pole-regression.json",JsonConvert.SerializeObject(new{maxPoleDrift,legacyPoleFailures}));
            CheckPhysics(check);
            Directory.CreateDirectory(".utmp/aim-repair");
            var report=new{state="passed",assertions=passed.Count,maxGeometryErrorDegrees=maxError,passed=passed};
            File.WriteAllText(".utmp/aim-repair/geometry-validation.json",JsonConvert.SerializeObject(report,Formatting.Indented));
            return JsonConvert.SerializeObject(new{report.state,report.assertions,report.maxGeometryErrorDegrees});
        }

        private static void CheckPhysics(Action<bool,string> check)
        {
            var original=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var player=new GameObject("AimQuerySelf");
                var self=player.AddComponent<BoxCollider>();
                player.transform.position=new Vector3(5000,100,5001);
                var trigger=new GameObject("AimQueryTrigger");
                trigger.transform.position=new Vector3(5000,100,5002);
                trigger.AddComponent<BoxCollider>().isTrigger=true;
                var obstacle=new GameObject("AimQueryObstacle");
                obstacle.transform.position=new Vector3(5000,100,5004);
                obstacle.AddComponent<BoxCollider>();
                Physics.SyncTransforms();
                var query=new UnityAimWorldQuery();
                query.SetIgnoredColliders(new[]{self.GetInstanceID()});
                var origin=new Vector3(5000,100,5000);
                check(query.Raycast(origin,Vector3.forward,10,-1,out var hit) && Mathf.Abs(hit.z-5003.5f)<0.01f,"physics excludes self and trigger");
                check(query.IsInsideObstacle(obstacle.transform.position,0.01f,-1),"physics internal origin");
                check(!query.IsInsideObstacle(player.transform.position,0.01f,-1),"own internal collider ignored");
                query.Clear();
                check(query.Raycast(origin,Vector3.forward,10,-1,out hit) && Mathf.Abs(hit.z-5000.5f)<0.01f,"filter cleared");
            }
            finally
            {
                SceneManager.SetActiveScene(original);
                EditorSceneManager.CloseScene(scene,true);
            }
        }

        public sealed class AimGeometryTestArchitecture : Architecture<AimGeometryTestArchitecture>
        {
            protected override void Init()
            {
                RegisterModel(new AimModel());
                RegisterUtility<IAimWorldQuery>(new AimTestWorld());
                RegisterSystem<IAimSystem>(new AimSystem());
            }
        }

        private sealed class AimTestWorld : IAimWorldQuery
        {
            public Vector3? CameraTarget, Obstruction;
            public bool CameraInside, MuzzleInside;
            public int Calls, IgnoredCount;
            public void Reset() { CameraTarget=Obstruction=null; CameraInside=MuzzleInside=false; Calls=0; }
            public void SetIgnoredColliders(int[] ids) { IgnoredCount=ids==null?0:ids.Length; }
            public bool Raycast(Vector3 origin,Vector3 direction,float distance,int mask,out Vector3 point)
            {
                Calls++;
                var value=origin.z<0 ? CameraTarget : Obstruction;
                point=value??Vector3.zero;
                return value.HasValue;
            }
            public bool IsInsideObstacle(Vector3 origin,float radius,int mask)
            { Calls++; return origin.z<0 ? CameraInside : MuzzleInside; }
            public void Clear() { Reset(); IgnoredCount=0; }
        }
    }
}