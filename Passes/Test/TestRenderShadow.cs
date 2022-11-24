using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT + "/Test/" + nameof(TestRenderShadow))]
    internal class TestRenderShadow : BasePass
    {
        readonly int _MainLightShadowMap = Shader.PropertyToID(nameof(_MainLightShadowMap));
        readonly int _MainLightShadowMapMatrices = Shader.PropertyToID(nameof(_MainLightShadowMapMatrices));

        public int atlasSize = 1024;
        [Range(1,4)]public int cascadeCount = 1;
        public Vector3 splitRatio = new Vector3(0.1f, 0.3f, 0.6f);

        Matrix4x4[] shadowMapMatrices;
        public override void OnRender()
        {
            if (!IsCullingResultsValid())
                return;

            if(shadowMapMatrices == null)
                shadowMapMatrices= new Matrix4x4[4];

            Cmd.GetTemporaryRT(_MainLightShadowMap, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            Cmd.SetRenderTarget(_MainLightShadowMap,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
            Cmd.ClearRenderTarget(true, false, Color.clear);

            for (int i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                var vLight = cullingResults.visibleLights[i];
                var light = vLight.light;
                if(light.shadows == LightShadows.None ||
                    light.shadowStrength<=0 ||
                    !cullingResults.GetShadowCasterBounds(i,out var bounds)
                    )
                {
                    continue;
                }

                var split = cascadeCount<=1 ? 1 : 2;

                var tileSize = atlasSize/split;

                for (int j = 0; j < cascadeCount; j++)
                {
                    RenderDirShader(i, j, split, tileSize, light.shadowNearPlane);
                }
                break;
            }

            Cmd.SetGlobalMatrixArray(_MainLightShadowMapMatrices, shadowMapMatrices);
            Cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            Cmd.SetGlobalDepthBias(0, 0);
        }

        void RenderDirShader(int lightIndex,int cascadeIndex,int splitCount,int tileSize, float shadowNearPlane)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex, cascadeIndex, cascadeCount,
               splitRatio, tileSize,shadowNearPlane,
                out var viewMat, out var projMat, out var splitData);

            var rowId = cascadeIndex / splitCount;
            var colId = cascadeIndex % splitCount;
            var viewportRect = new Rect(colId*tileSize,rowId * tileSize,tileSize,tileSize);
            Cmd.SetViewport(viewportRect);
            Cmd.SetViewProjectionMatrices(viewMat, projMat);

            ExecuteCommand();
            
            var settings = new ShadowDrawingSettings(cullingResults, 0);
            settings.splitData = splitData;
            
            context.DrawShadows(ref settings);

            //save 
            shadowMapMatrices[cascadeIndex] = ToTextureMatrix(projMat * viewMat,splitCount,
                colId,rowId);
        }

        /// <summary>
        /// clip space [-1,1] -> texture space [0,1]
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        Matrix4x4 ToTextureMatrix(Matrix4x4 m,int splitCount,float x,float y)
        {
            if(SystemInfo.usesReversedZBuffer)
            {
                m.SetRow(2, -m.GetRow(2));
            }
            var splitRatio = 1f / splitCount;
            var mat = MatrixEx.Matrix(
                0.5f * splitRatio, 0, 0, 0.5f * splitRatio + splitRatio * x,
                0, 0.5f * splitRatio, 0, 0.5f * splitRatio + splitRatio * y,
                0, 0, 0.5f, 0.5f,
                0, 0, 0, 1
                );
            mat.Matrix(
                0.5f, 0, 0, 0.5f,
                0, 0.5f, 0, 0.5f,
                0, 0, 0.5f, 0.5f,
                0, 0, 0, 1
                );
            return mat * m;

        }
    }
}
