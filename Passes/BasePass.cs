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

        [Header("Filters (all)")]
        [Tooltip(" Execute when camera's tag equals,otherwise skip")]
        public string gameCameraTag;

        [Header("Render flow")]
        [Tooltip("When pass done, break render flow?")]
        public bool isInterrupt;

        [Tooltip("Skip this pass?")]
        public bool isSkip;

        public bool isReset;

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
        protected int executeCount;

        public void ExecuteCommand()
        {
            context.ExecuteCommandBuffer(Cmd);
            Cmd.Clear();
        }

        public void Render(ref ScriptableRenderContext context,Camera camera)
        {
            TryReset();

            this.context = context;
            this.camera = camera;
            var passName = PassName();

            if (!CanExecute())
                return;
            
            TryInit();

            //if(camera.IsReflectionCamera())
            //Debug.Log(passName);

            Cmd.name = passName;
            Cmd.BeginSampleExecute(Cmd.name, ref context);

            OnRender();

            Cmd.name = passName;
            Cmd.EndSampleExecute(Cmd.name, ref context);
        }

        /// <summary>
        /// init once 
        /// </summary>
        private void TryInit()
        {
            if (executeCount == 0)
            {
                Init();
            }
            executeCount++;
        }

        private void TryReset()
        {
            if (isReset)
            {
                isReset = false;
                executeCount = 0;
            }
        }

        public virtual bool CanExecute()
        {
            if (camera.cameraType == CameraType.Game)
            {
                return string.IsNullOrEmpty(gameCameraTag) ? true : camera.CompareTag(gameCameraTag);
            }
            return true;
            //return enabledCameraTypes.HasFlag(camera.cameraType);
        }

        public bool IsCullingResultsValid() => cullingResults != default;
        public abstract void OnRender();


        public virtual string PassName() => string.IsNullOrEmpty(overridePassName) ? GetType().Name : overridePassName;
        public virtual bool NeedCleanup() => false;

        /// <summary>
        /// when NeedCleanup() is true
        /// call Cleanup when renderPipeline is done.
        /// </summary>
        public virtual void Cleanup() { }

        /// <summary>
        /// only call once before OnRender
        /// set isReset = true,will reset pass
        /// </summary>
        public virtual void Init() { }
    }
}
