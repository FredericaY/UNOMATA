using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cinemachine;
using Newtonsoft.Json;
using QFramework;
using StarterAssets;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Unomata.Gameplay;
using Object=UnityEngine.Object;
namespace Unomata.Editor.Validation
{
    public static class AimInputSceneValidation
    {
        private static string _state="idle",_error;private static bool _running,_cleaned;
        private static readonly List<string> Checks=new List<string>();
        private static PlayerMotor _motor;private static PlayerAimPresentation _view;private static StarterAssetsInputs _buffer;
        private static PlayerInputModel _input;private static int _jumps,_landStart,_inputFrame=-1,_sameFrame=-1,_samples;private static float _maxHeight,_groundY;
        private static int _clothValid,_clothLate;private static MagicaCloth2.MagicaCloth _cloth;
        public static string Status()=>JsonConvert.SerializeObject(new{state=_state,error=_error,checks=Checks.Count,jumps=_jumps,sameFrame=_sameFrame,samples=_samples,maxHeight=_maxHeight,clothValid=_clothValid,clothLate=_clothLate,cleanupComplete=_cleaned});
        public static string Start(){if(!EditorApplication.isPlaying||_running)throw new InvalidOperationException("Requires idle Play Mode.");_running=true;_cleaned=false;_state="running";_error=null;Checks.Clear();_jumps=_samples=_clothValid=_clothLate=0;_sameFrame=_inputFrame=-1;_maxHeight=0;Run();return Status();}
        public static void Cancel(){_running=false;}
        private static void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);Checks.Add(label);}
        private static async Task Frames(int count)
        {int end=Time.frameCount+count;double deadline=EditorApplication.timeSinceStartup+15;while(Time.frameCount<end){if(!_running||!EditorApplication.isPlaying)throw new OperationCanceledException();if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException();await Task.Delay(8);}}
        private static void Sample()
        {
            _samples++;if(_motor.JumpTriggered)_jumps++;_maxHeight=Mathf.Max(_maxHeight,_motor.transform.position.y-_groundY);
            if(_inputFrame==Time.frameCount&&_motor.MovementFrame==Time.frameCount&&_input.Move.Value.y>0&&_input.Sprint.Value&&_motor.PlanarSpeed>0)_sameFrame=Time.frameCount;
            if(_cloth!=null&&_cloth.IsValid())_clothValid++;
            if(_view.PoseFrame==Time.frameCount&&MagicaCloth2.MagicaManager.GetUpdateLocation()==MagicaCloth2.TimeManager.UpdateLocation.AfterLateUpdate)_clothLate++;
        }
        private static async void Run()
        {
            Keyboard keyboard=null;Mouse mouse=null;PlayerInput pi=null;InputDevice[] devices=null;string scheme=null;bool auto=false,background=Application.runInBackground;
            int fps=Application.targetFrameRate,vSync=QualitySettings.vSyncCount;Vector3 position=default;Quaternion rotation=default;Action<InputAction.CallbackContext> moved=null;
            PlayerController pc=null;SAInputAdapter adapter=null;bool pcEnabled=false,adapterEnabled=false;int lands=0;
            try
            {
                _view=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<PlayerAimPresentation>()).Single();_motor=_view.Motor;
                _buffer=_motor.GetComponent<StarterAssetsInputs>();_input=GameApp.Interface.GetModel<PlayerInputModel>();pi=_motor.GetComponent<PlayerInput>();
                pc=_motor.GetComponent<PlayerController>();adapter=_motor.GetComponent<SAInputAdapter>();pcEnabled=pc.enabled;adapterEnabled=adapter.enabled;
                position=_motor.transform.position;rotation=_motor.transform.rotation;_groundY=position.y;_landStart=_motor.LandingSequence;
                _cloth=_motor.GetComponentInChildren<MagicaCloth2.MagicaCloth>();
                devices=pi.devices.ToArray();scheme=pi.currentControlScheme;auto=pi.neverAutoSwitchControlSchemes;
                Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
                keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();pi.neverAutoSwitchControlSchemes=true;
                pi.SwitchCurrentControlScheme("KeyboardMouse",keyboard,mouse);pc.enabled=adapter.enabled=true;
                pc.SendMessage("OnApplicationFocus",true);adapter.SendMessage("OnApplicationFocus",true);_view.SendMessage("OnApplicationFocus",true);
                moved=ctx=>{if(ctx.ReadValue<Vector2>().y>0)_inputFrame=Time.frameCount;};pi.actions.FindAction("Move").performed+=moved;
                MagicaCloth2.MagicaManager.afterLateUpdateDelegate+=Sample;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState());await Frames(35);
                Check(_cloth!=null&&_cloth.IsValid(),"cloth initializes before motion sampling");
                _samples=_clothValid=_clothLate=0;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));await Frames(4);
                Check(_sameFrame>=0,"real PlayerInput Move/Sprint consumed by motor in callback frame");
                InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right));await Frames(35);
                Check(_motor.PlanarSpeed>5.2f,"aim W+Shift retains forward sprint");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A,Key.LeftShift));await Frames(2);
                Check(_motor.PlanarSpeed<=2.002f&&_input.Sprint.Value,"aim A+held Shift immediately walks without clearing input");
                await Frames(20);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S,Key.LeftShift));await Frames(20);
                Check(_motor.PlanarSpeed<=2.002f&&_motor.PlanarSpeed>1.9f,"aim S+Shift walks");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D,Key.LeftShift));await Frames(20);
                Check(_motor.PlanarSpeed<=2.002f,"aim W+D+Shift walks");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));await Frames(35);
                Check(_motor.PlanarSpeed>5.2f&&_input.Sprint.Value,"held Shift resumes sprint when only W remains");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A,Key.LeftShift));await Frames(2);
                InputSystem.QueueStateEvent(mouse,new MouseState());await Frames(35);
                Check(_motor.PlanarSpeed>5.2f,"release aim restores side-direction free sprint");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(45);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));await Frames(180);
                Check(_jumps==1&&_motor.LandingSequence-_landStart==1,"held Jump crosses landing without repeat");
                Check(_maxHeight>1&&_maxHeight<1.5f,"jump height preserves physics baseline");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(4);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));await Frames(110);
                Check(_jumps==2&&_motor.LandingSequence-_landStart==2,"release and re-press produces exactly one further jump");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right));await Frames(30);
                Check(_input.IsAiming.Value&&_view.AimWeight>0.99f,"real right-button action reaches aim pose");
                pi.DeactivateInput();await Frames(25);Check(!_input.IsAiming.Value&&_view.AimWeight==0,"input source deactivation exits aim");
                pi.ActivateInput();InputSystem.QueueStateEvent(mouse,new MouseState());await Frames(5);
                InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right));await Frames(25);Check(_view.AimWeight>0.99f,"input source reactivation recovers aim");
                pi.currentActionMap.Disable();await Frames(25);Check(!_input.IsAiming.Value&&_buffer.move==Vector2.zero,"map disable clears live scene input");
                pi.currentActionMap.Enable();InputSystem.QueueStateEvent(mouse,new MouseState());await Frames(5);
                Check(_clothValid==_samples&&_clothLate==_samples,"cloth remains valid and consumes final same-frame pose");
                Check(_motor.GetComponents<ThirdPersonController>().Length==0,"only project motor consumes movement");
                lands=_motor.LandingSequence-_landStart;_state="passed";
            }
            catch(Exception ex){_state=ex is OperationCanceledException?"cancelled":"failed";_error=ex.ToString();}
            finally
            {
                MagicaCloth2.MagicaManager.afterLateUpdateDelegate-=Sample;
                if(pi!=null&&moved!=null)pi.actions.FindAction("Move").performed-=moved;
                if(pi!=null){pi.ActivateInput();if(devices!=null&&devices.Length>0)pi.SwitchCurrentControlScheme(scheme,devices);pi.neverAutoSwitchControlSchemes=auto;}
                if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);
                if(_motor!=null){lands=_motor.LandingSequence-_landStart;var cc=_motor.GetComponent<CharacterController>();cc.enabled=false;_motor.transform.SetPositionAndRotation(position,rotation);cc.enabled=true;_buffer.move=_buffer.look=Vector2.zero;_buffer.jump=_buffer.sprint=false;GameApp.Interface.SendCommand(new ResetPlayerInputCommand());}
                if(pc!=null){pc.enabled=pcEnabled;pc.SendMessage("OnApplicationFocus",Application.isFocused);}if(adapter!=null){adapter.enabled=adapterEnabled;adapter.SendMessage("OnApplicationFocus",Application.isFocused);}if(_view!=null)_view.SendMessage("OnApplicationFocus",Application.isFocused);
                Application.runInBackground=background;Application.targetFrameRate=fps;QualitySettings.vSyncCount=vSync;_running=false;_cleaned=true;
                Directory.CreateDirectory(".utmp/aim-repair");File.WriteAllText(".utmp/aim-repair/input-scene-report.json",JsonConvert.SerializeObject(new{state=_state,error=_error,checks=Checks,jumps=_jumps,lands,sameFrame=_sameFrame,samples=_samples,maxHeight=_maxHeight,clothValid=_clothValid,clothLate=_clothLate,cleanupComplete=_cleaned},Formatting.Indented));
            }
        }
    }
}