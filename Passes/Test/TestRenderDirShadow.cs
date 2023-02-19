using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities.CRP
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT + "/Test/" + nameof(TestRenderDirShadow))]
    internal class TestRenderDirShadow : BasePass
    {
        readonly int _MainLightShadowMap = Shader.PropertyToID(nameof(_MainLightShadowMap));
        readonly int _MainLightShadowMapMatrices = Shader.PropertyToID(nameof(_MainLightShadowMapMatrices));
        int _MainLightCascadeCullingSpheres = Shader.PropertyToID(nameof(_MainLightCascadeCullingSpheres));

        public int atlasSize = 1024;
        [Range(1,4)]public int cascadeCount = 1;
        public Vector3 splitRatio = new Vector3(0.1f, 0.3f, 0.6f);

        Matrix4x4[] shadowMapMatrices;
        Vector4[] cascadeCullingSpheres = new Vector4[4];
        public override void OnRender()
        {
            if (!IsCullingResultsValid())
                return;

            if (shadowMapMatrices == null)
                shadowMapMatrices = new Matrix4x4[4*4];

            Cmd.GetTemporaryRT(_MainLightShadowMap, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            Cmd.SetRenderTarget(_MainLightShadowMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            Cmd.ClearRenderTarget(true, false, Color.clear);

            //RenderDirLights();
            //RenderShadowsWithCascade();
            RenderMainLightWithCascade();

            Cmd.SetGlobalMatrixArray(_MainLightShadowMapMatrices, shadowMapMatrices);
            Cmd.SetGlobalVectorArray(_MainLightCascadeCullingSpheres, cascadeCullingSpheres);
            Cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            Cmd.SetGlobalDepthBias(0, 0);
        }

        private void RenderDirLights()
        {
            for (int i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                var vLight = cullingResults.visibleLights[i];
                var light = vLight.light;
                if (light.shadows == LightShadows.None ||
                    light.shadowStrength <= 0 ||
                    !cullingResults.GetShadowCasterBounds(i, out var bounds)
                    )
                {
                    continue;
                }

                var split = cullingResults.visibleLights.Length <= 1 ? 1 : 2;

                var tileSize = atlasSize / split;

                RenderDirShadow(i, split, tileSize, light.shadowNearPlane);
                //break;
            }
        }

        void RenderDirShadow(int lightIndex,int splitCount,int tileSize, float shadowNearPlane)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex, 0, 1,
               splitRatio, tileSize,shadowNearPlane,
                out var viewMat, out var projMat, out var splitData);

            var rowId = lightIndex / splitCount;
            var colId = lightIndex % splitCount;
            var viewportRect = new Rect(colId*tileSize,rowId * tileSize,tileSize,tileSize);
            Cmd.SetViewport(viewportRect);
            Cmd.SetViewProjectionMatrices(viewMat, projMat);

            ExecuteCommand();
            
            var settings = new ShadowDrawingSettings(cullingResults, 0);
            settings.splitData = splitData;
            
            context.DrawShadows(ref settings);

            //save 
            shadowMapMatrices[lightIndex] = ToTextureMatrix(projMat * viewMat,splitCount,
                colId,rowId);
        }

        void RenderMainLightWithCascade()
        {
            var cascadeSplit = cascadeCount <= 1 ? 1 : 2;
            var tileSize = atlasSize / cascadeSplit;

            for (int i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                var vlight = cullingResults.visibleLights[i];
                var light = vlight.light;
                if (light.shadowStrength <= 0 ||
                    light.shadows == LightShadows.None ||
                    ! cullingResults.GetShadowCasterBounds(i,out var bounds)
                    )
                    continue;

                for (int j = 0; j < cascadeCount; j++)
                {
                    RenderDirShadowCascade(i, j, cascadeSplit, tileSize, light.shadowNearPlane, 1, 0, 0);
                }
                break;
            }
        }

        void RenderShadowsWithCascade()
        {
            var lightSplit = cullingResults.visibleLights.Length <= 1 ? 1 : 2;
            var cascadeSplit = cascadeCount <= 1 ? 1 : 2;
            var tileSize = atlasSize / (lightSplit * cascadeSplit);

            for (int i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                var vLight = cullingResults.visibleLights[i];
                var light = vLight.light;
                if (light.shadows == LightShadows.None ||
                    light.shadowStrength <= 0 ||
                    !cullingResults.GetShadowCasterBounds(i, out var bounds)
                    )
                {
                    continue;
                }

                var lightRowId = i / lightSplit;
                var lightColId = i % lightSplit;

                for (int j = 0; j < cascadeCount; j++)
                {
                    RenderDirShadowCascade(i, j, cascadeSplit, tileSize, light.shadowNearPlane,lightSplit, lightRowId*cascadeSplit, lightColId * cascadeSplit);
                }
            }
        }

        void RenderDirShadowCascade(int lightIndex, int cascadeId,int splitCount, int tileSize, float shadowNearPlane,int lightSplit,int lightRowId,int lightColId)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex, cascadeId, cascadeCount,
               splitRatio, tileSize, shadowNearPlane,
                out var viewMat, out var projMat, out var splitData);

            var rowId = cascadeId / splitCount + lightRowId;
            var colId = cascadeId % splitCount + lightColId;
            var viewportRect = new Rect(colId* tileSize, rowId * tileSize, tileSize, tileSize);
            //Debug.Log(string.Format("{}",viewportRect);
            Cmd.SetViewport(viewportRect);
            Cmd.SetViewProjectionMatrices(viewMat, projMat);

            ExecuteCommand();

            var settings = new ShadowDrawingSettings(cullingResults, lightIndex);
            settings.splitData = splitData;

            context.DrawShadows(ref settings);

            //save 
            //var splitId = 
            shadowMapMatrices[cascadeId] = ToTextureMatrix(projMat * viewMat, splitCount* lightSplit,
                colId, rowId);
            cascadeCullingSpheres[cascadeId] = splitData.cullingSphere;
        }

        /// <summary>
        /// clip space [-1,1] -> texture space [0,1]
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Matrix4x4 ToTextureMatrix(Matrix4x4 m,int splitCount,float x,float y)
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

            return mat * m;
        }
    }
}
