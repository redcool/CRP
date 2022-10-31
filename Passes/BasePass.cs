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
        public bool isFoldout;

        [Header("Pass Options")]
        public string log;

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
            BeginSample(Cmd.name);

            OnRender();
            EndSample(Cmd.name);
        }

        public void BeginSample(string name)
        {
            Cmd.name = name;
            Cmd.BeginSample(name);
            ExecuteCommand();
        }

        public void EndSample(string name)
        {
            Cmd.EndSample(name);
            ExecuteCommand();
        }

        public abstract void OnRender();
        public virtual bool CanExecute() => ! isSkip;
        public virtual string PassName() => nameof(BasePass);
    }
}
