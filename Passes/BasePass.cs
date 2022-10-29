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
        [Header("Pass Options")]
        public string log;



        public bool isInterrupt;
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
            this.context = context;
            this.camera = camera;

            Cmd.name = camera.name;

            BeginSample(cmd.name);

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
    }
}
