using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    public enum CameraTypeComparison
    {
        LessEquals,
        Less,
        Equals
    }
    public enum GameObjectTagComparison
    {
        Equals,
        NotEquals,
    }


    public abstract class BasePass : ScriptableObject
    {
        public enum PassRunMode
        {
            Normal = 0,
            Interrupt = 1,
            Skip = 2,
            EditorOnly = 3
        }


        [Header(nameof(BasePass))]
        public bool isFoldout;

        [Tooltip("Pass name show in FrameDebugger")]
        public string overridePassName;

        [Header("Filter (GameObject Tag)")]
        [Tooltip("Execute when Game camera's tag equals,otherwise skip")]
        public string gameCameraTag;
        public GameObjectTagComparison gameCameraTagComparison;

        [Header("Filter (CameraType)")]
        [Tooltip("Execute when current camera type <= minimalCameraType")]
        public CameraType minimalCameraType = CameraType.Reflection;
        [Tooltip("camera type comparison")]
        public CameraTypeComparison cameraTypeComparison = CameraTypeComparison.LessEquals;

        [Header("Pass states")]
        [Tooltip("Skip : dont run , Interrupt : after run interrupt passes afterward , EditorOnly : run only in unity editor")]
        public PassRunMode passRunMode;
        public bool isEditorOnly;

        [Tooltip("ResetCounter will trigger Init() again")]
        public bool isResetCounter;

        [NonSerialized] public ScriptableRenderContext context;
        [NonSerialized] public Camera camera;

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
        private CRPCameraData cameraData;

        public void ExecuteCommand()
        {
            context.ExecuteCommandBuffer(Cmd);
            Cmd.Clear();
        }

        public void Render(ref ScriptableRenderContext context, Camera camera)
        {
            TryReset();

            this.context = context;
            this.camera = camera;
            var passName = PassName();

            TryInit();

            if (!CanExecute())
                return;

            //if(camera.IsReflectionCamera())
            //Debug.Log(passName);

            Cmd.name = passName;
            Cmd.BeginSampleExecute(Cmd.name, ref context);

            OnRender();

            Cmd.name = passName;
            Cmd.EndSampleExecute(Cmd.name, ref context);
        }

        /// <summary>
        /// Init once before OnRender
        /// </summary>
        void TryInit()
        {
            if (executeCount == 0)
            {
                Init();
            }
            executeCount++;
        }

        void TryReset()
        {
            if (isResetCounter)
            {
                isResetCounter = false;
                executeCount = 0;
            }
        }

        bool IsValidCameraType(CameraTypeComparison comp, CameraType cameraType, CameraType minCameraType) => comp switch
        {
            CameraTypeComparison.Equals => cameraType == minCameraType,
            CameraTypeComparison.Less => cameraType < minCameraType,
            _ => cameraType <= minimalCameraType,
        };

        bool IsValidGameObjectTag(GameObjectTagComparison comp, Camera cam, string tag) => comp switch
        {
            GameObjectTagComparison.NotEquals => !cam.CompareTag(tag),
            _ => cam.CompareTag(tag),
        };

        public virtual bool CanExecute()
        {
            if (camera.cameraType == CameraType.Game)
            {
                return string.IsNullOrEmpty(gameCameraTag) ? true :
                    IsValidGameObjectTag(gameCameraTagComparison, camera, gameCameraTag);
            }

            var isValidPass = IsValidCameraType(cameraTypeComparison, camera.cameraType, minimalCameraType);

            if (isEditorOnly)
                isValidPass = isValidPass && Application.isEditor;

            return isValidPass;
        }
        /// <summary>
        /// SetupCamera should prefered call , when done return true
        /// </summary>
        /// <returns></returns>
        public bool IsCullingResultsValid() => cullingResults != default;

        public abstract void OnRender();


        public virtual string PassName() => string.IsNullOrEmpty(overridePassName) ? name : overridePassName;

        /// <summary>
        /// when IsNeedPipelineCleanup() is true
        /// call PipelineCleanup when renderPipeline is done.
        /// </summary>
        public virtual bool IsNeedPipelineCleanup() => false;

        /// <summary>
        /// when IsNeedPipelineCleanup() is true
        /// call PipelineCleanup when renderPipeline is done.
        /// </summary>
        public virtual void PipelineCleanup() { }

        /// <summary>
        /// when IsNeedCameraCleanup() is true
        /// call CameraCleanup when camera rendering is done.
        /// </summary>
        public virtual bool IsNeedCameraCleanup() => false;
        /// <summary>
        /// when IsNeedCameraCleanup() is true
        /// call CameraCleanup when camera rendering is done.
        /// </summary>
        public virtual void CameraCleanup() { }

        /// <summary>
        /// only call once before OnRender
        /// set isReset = true,will reset pass
        /// </summary>
        public virtual void Init() { }

        public bool IsSkip() => passRunMode== PassRunMode.Skip;
        public bool IsInterrupt() => passRunMode== PassRunMode.Interrupt;


        public CRPCameraData CameraData
        {
            get
            {
                if (!cameraData)
                    cameraData = camera.GetComponent<CRPCameraData>();
                return cameraData;
            }
        }
    }
}
