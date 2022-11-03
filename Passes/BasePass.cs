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
        public string overridePassName;

        [Tooltip("when pass done, interrupt flow afterwards")]
        public bool isInterrupt;

        [Tooltip("skip this pass?")]
        public bool isSkip;

        [NonSerialized]public ScriptableRenderContext context;
        [NonSerialized]public Camera camera;

        static CommandBuffer cmd;
        public static CommandBuffer Cmd
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
            if (!CanExecute())
                return;
            this.context = context;
            this.camera = camera;

            Cmd.name = PassName();
            Cmd.BeginSampleExecute(Cmd.name, ref context);

            OnRender();

            Cmd.EndSampleExecute(Cmd.name, ref context);
        }


        public abstract void OnRender();
        public virtual bool CanExecute() => ! isSkip;
        public virtual string PassName() => string.IsNullOrEmpty(overridePassName) ? GetType().Name : overridePassName;
    }
}
