using UnityEngine;
using UnityEngine.Rendering;

namespace PowerUtilities.CRP
{
    [CreateAssetMenu(menuName = CRP.CREATE_PASS_ASSET_MENU_ROOT + "/Test/" + nameof(TestRenderOtherShadow))]
    public class TestRenderOtherShadow : BasePass
    {
        struct OtherShadowInfo
        {
            public int lightIndex;
            public float strength;
            public float bias;
            public float normalBias;
            public bool isPoint;
        }
        int _OtherShadowMap = Shader.PropertyToID(nameof(_OtherShadowMap));
        int _OtherShadowMatrices = Shader.PropertyToID(nameof(_OtherShadowMatrices));
        int _OtherShadowData = Shader.PropertyToID(nameof(_OtherShadowData));
        int _OtherLightCount = Shader.PropertyToID(nameof(_OtherLightCount));
        int _OtherLightPositions = Shader.PropertyToID(nameof(_OtherLightPositions));
        int _OtherLightDirections = Shader.PropertyToID(nameof(_OtherLightDirections));

        Matrix4x4[] otherShadowMatrices;
        Vector4[] otherShadowData; //{light index,}
        Vector4[] otherLightPositions, otherLightDirections;

        const int atlasSize = 1024;
        const int maxShadowedLightCount = 16;
        const int maxOtherLightCount = 64;
        OtherShadowInfo[] shadowInfos = new OtherShadowInfo[maxShadowedLightCount];



        public override void OnRender()
        {
            if (!IsCullingResultsValid())
                return;

            Cmd.GetTemporaryRT(_OtherShadowMap, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            Cmd.SetRenderTarget(_OtherShadowMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            Cmd.ClearRenderTarget(true, false, Color.clear);

            if (otherShadowData == null || otherShadowData.Length != maxOtherLightCount)
            {
                otherShadowData = new Vector4[maxOtherLightCount];
                otherLightDirections = new Vector4[maxOtherLightCount];
                otherLightPositions = new Vector4[maxOtherLightCount];
            }
            var lightCount = SetupLightInfos();
            var shadowedLightCount = SetupShadowInfos();
            RenderShadowLights(shadowedLightCount);

            Cmd.SetGlobalInt(_OtherLightCount, lightCount);
            Cmd.SetGlobalVectorArray(_OtherShadowData, otherShadowData);
            Cmd.SetGlobalVectorArray(_OtherLightDirections, otherLightDirections);
            Cmd.SetGlobalVectorArray(_OtherLightPositions, otherLightPositions);
        }

        void RenderShadowLights(int shadowedLightCount)
        {
            if(otherShadowMatrices == null || otherShadowMatrices.Length != maxShadowedLightCount)
            {
                otherShadowMatrices = new Matrix4x4[maxShadowedLightCount];
            }

            var split = shadowedLightCount <=1 ? 1 : shadowedLightCount <=4 ? 2 : 4;
            var tileSize = atlasSize / split;

            for (int i = 0; i < shadowedLightCount;)
            {
                var shadowInfo = shadowInfos[i];

                if (shadowInfo.isPoint)
                {
                    RenderPointShadow(shadowInfo,i,split,tileSize);
                }
                else
                {
                    RenderSpotShadow(shadowInfo, i, split, tileSize);
                }
                i += shadowInfo.isPoint ? 6 : 1;
            }

            Cmd.SetGlobalDepthBias(0, 0);
            Cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        }

        private void RenderPointShadow(OtherShadowInfo shadowInfo,int id,int split,int tileSize)
        {
            var settings = new ShadowDrawingSettings(cullingResults, shadowInfo.lightIndex);

            var fovBias = 0;

            for (int i = 0; i < 6; i++)
            {
                cullingResults.ComputePointShadowMatricesAndCullingPrimitives(shadowInfo.lightIndex, (CubemapFace)i, fovBias,
                    out var viewMatirx, out var projMatrix, out var splitData);
                settings.splitData = splitData;

                var index = id + i;
                var offset = new Vector2(index % split,index/ tileSize);
                var viewportRect = new Rect(offset.x * tileSize,offset.y * tileSize,tileSize,tileSize);
                Cmd.SetViewport(viewportRect);
                Cmd.SetViewProjectionMatrices(viewMatirx, projMatrix);
                Cmd.SetGlobalDepthBias(0, shadowInfo.bias);
                ExecuteCommand();

                context.DrawShadows(ref settings);
                //
                otherShadowMatrices[index] = TestRenderDirShadow.ToTextureMatrix(projMatrix * viewMatirx, split, offset.x, offset.y);
            }
        }

        void RenderSpotShadow(OtherShadowInfo shadowInfo, int id, int split, int tileSize)
        {
            var settings = new ShadowDrawingSettings(cullingResults, shadowInfo.lightIndex);
            cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(shadowInfo.lightIndex,
                out var viewMatrix, out var projMatrix, out var splitData);
            settings.splitData = splitData;

            var offset = new Vector2(id % split, id / split);
            var viewportRect =new Rect(offset.x * tileSize,offset.y * tileSize, tileSize, tileSize);

            Cmd.SetViewport(viewportRect);
            Cmd.SetGlobalDepthBias(0, shadowInfo.bias);
            Cmd.SetViewProjectionMatrices(viewMatrix, projMatrix);

            ExecuteCommand();
            context.DrawShadows(ref settings);
            //
            otherShadowMatrices[id] = TestRenderDirShadow.ToTextureMatrix(projMatrix * viewMatrix, split, offset.x, offset.y);
        }

        int SetupLightInfos()
        {
            var lightCount = 0;

            for (int i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                var vLight = cullingResults.visibleLights[i];
                var isPoint = vLight.lightType == LightType.Point;
                if (!(isPoint || vLight.lightType == LightType.Spot))
                    continue;

                if (lightCount >= maxOtherLightCount)
                    break;

                otherLightPositions[lightCount] = vLight.localToWorldMatrix.GetColumn(3);
                otherLightDirections[lightCount] = -vLight.localToWorldMatrix.GetColumn(2);
                //otherShadowData[lightCount] = new Vector4(i, isPoint ? 1 : 0);

                lightCount++;
            }

            return lightCount;
        }

        int SetupShadowInfos()
        {
            int shadowedOtherLightCount = 0;
            for (int i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                var vLight = cullingResults.visibleLights[i];
                var isPoint = vLight.lightType == LightType.Point;
                if (!(isPoint || vLight.lightType == LightType.Spot))
                    continue;
                if (vLight.light.shadowStrength <= 0 || !cullingResults.GetShadowCasterBounds(i, out var bounds))
                    continue;
                if (shadowedOtherLightCount >= maxShadowedLightCount)
                    break;

                shadowInfos[shadowedOtherLightCount] = new OtherShadowInfo
                {
                    bias = vLight.light.shadowBias,
                    normalBias = vLight.light.shadowNormalBias,
                    lightIndex = i,
                    strength = vLight.light.shadowStrength,
                    isPoint = isPoint,
                };

                otherShadowData[i] = new Vector4(shadowedOtherLightCount, isPoint ? 1 : 0);

                shadowedOtherLightCount += isPoint ? 6 : 1;
            }

            return shadowedOtherLightCount;
        }
    }
}
