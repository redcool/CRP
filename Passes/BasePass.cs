using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    public abstract class BasePass : ScriptableObject
    {
        [Header(nameof(BasePass))]
        public bool isFoldout;

        [Tooltip("Pass name show in FrameDebugger")]
        public string overridePassName;

        [Header("Filters")]
        [Tooltip("Execute when camera's tag equals,otherwise skip")]
        public string cameraTag;

        [Header("Render flow")]
        [Tooltip("When pass done, break render flow?")]
        public bool isInterrupt;

        [Tooltip("Skip this pass?")]
        public bool isSkip;

        [NonSerialized]public ScriptableRenderContext context;
        [NonSerialized]public Camera camera;

        static CommandBuffer cmd;
        //protected static ScriptableCullingParameters cullingParams;
        protected static CullingResults cullingResults;
        protected static CommandBuffer Cmd
        {
            get
            {
                if (cmd == null)
                    cmd =new CommandBuffer();
                return cmd;
            }
        }

        public void ExecuteCommand()
        {
            context.ExecuteCommandBuffer(Cmd);
            Cmd.Clear();
        }

        public void Render(ref ScriptableRenderContext context,Camera camera)
        {
            this.context = context;
            this.camera = camera;

            if (!CanExecute())
                return;

            var passName = PassName();
            Cmd.name = passName;
            Cmd.BeginSampleExecute(Cmd.name, ref context);

            OnRender();

            Cmd.name = passName;
            Cmd.EndSampleExecute(Cmd.name, ref context);
        }

        public abstract void OnRender();

        public virtual bool CanExecute() => 
            string.IsNullOrEmpty(cameraTag) ? true : camera.CompareTag(cameraTag) || 
            camera.cameraType == CameraType.SceneView ||
            camera.cameraType == CameraType.Preview;

        public bool IsCullingResultsValid() => cullingResults != default(CullingResults);

        public virtual string PassName() => string.IsNullOrEmpty(overridePassName) ? GetType().Name : overridePassName;
    }
}
