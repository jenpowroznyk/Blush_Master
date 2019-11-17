using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using GrassFlow;

[ExecuteInEditMode]
[AddComponentMenu("Rendering/GrassFlow")]
public class GrassFlowRenderer : MonoBehaviour {

    [Tooltip("Maximum number of instances to render. This number gets used with the LOD system to decrease number of instances in the distance.")]
    [SerializeField] private int _instanceCount = 50;
    public int instanceCount {
        get { return _instanceCount; }
        set {
            _instanceCount = value;
            UpdateTransform();
        }
    }

    [Tooltip("Receive shadows on the grass. Can be expensive, especially with cascaded shadows on. (Requires the grass shader with depth pass to render properly)")]
    public bool receiveShadows = true;

    [Tooltip("Grass casts shadows. Fairly expensive option. (Also requires the grass shader with depth pass to render at all)")]
    [SerializeField] private bool _castShadows = false;
    [SerializeField] private ShadowCastingMode shadowMode;
    public bool castShadows {
        get { return _castShadows; }
        set {
            _castShadows = value;
            shadowMode = value ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
    }

    [Tooltip("This setting only effects the editor. Most of the time you're going to want this on as it prevents visual popping as scripts are recompiled and such. " +
        "You can turn it off to get a more accurate view of game performance, though really it hardly makes any difference.")]
    public bool updateBuffers = true;

    [Tooltip("When on, this will manually frustum cull LOD chunks. This can help prevent Unity from having to cull the many instances that might be rendered per chunk. " +
        "This may be faster in some situations but it doesn't take shadows into account and can make shadows pop out when chunks go offscreen. " +
        "Turning this off, Unity will cull the instances rendered in the chunks.")]
    public bool useManualCulling = false;

    [Tooltip("Enables the ability to paint grass color and parameters dynamically in both the editor and in game. If true it creates Rendertextures from supplied textures " +
        "that can be painted and saved.")]
    [SerializeField] private bool _enableMapPainting = false;
    public bool enableMapPainting {
        get { return _enableMapPainting; }
        set {
            _enableMapPainting = value;

            if (value) {
                MapSetup();
            } else {
                ReleaseDetailMapRTs();
            }

            UpdateShaders();
        }
    }


    [Tooltip("Texture that controls grass color. The alpha channel of this texture is used to control how the color gets applied. " +
        "If alpha is 1, the color is also multiplied by material color, if 0, material color is ignored. Inbetween values work too.")]
    public Texture2D colorMap;

    [Tooltip("Texture that controls various parameters of the grass. Red channel = density. Green channel = height, Blue channel = flattenedness. Alpha channel = wind strength.")]
    public Texture2D paramMap;

    [Tooltip("If true, an instance of the material will be created to render with. Important if you want to use the same material for multiple grasses but want them to have different textures etc.")]
    public bool useMaterialInstance = false;

    [Tooltip("Material to use to render the grass. The material should use one of the grassflow shaders.")]
    public Material grassMaterial;

    [Tooltip("Layer to render the grass on.")]
    public int renderLayer;

    [Tooltip("Mode this grass is for. Mesh will attach grass to the triangles of a mesh, terrain will attach grass to surface of a unity terrain object.")]
    public GrassRenderType renderType;

    [Tooltip("Amount to expand grass chunks on terrain, helps avoid artifacts on edges of chunks. Preferably set this as low as you can without it looking bad.")]
    public float terrainExpansion = 0.35f;
    

    [Tooltip("This is not necessary in most cases but sometimes terrains of extreme scale can benefit from this. " +
        "Uses DOUBLE the memory.")]
    public bool highQualityHeightmap = false;

    [Tooltip("Transform that the grass belongs to.")]
    public Transform terrainTransform;

    [Tooltip("Terrain object to attach grass to in terrain mode.")]
    public Terrain terrainObject;

    [Tooltip("Mesh to attach grass to in mesh mode.")]
    public Mesh grassMesh;

    [Tooltip("Amount of grass to render per mesh triangle in mesh mode. Technically controls the amount of grass per instance, per tri, meaning maximum total grass per tri = " +
                    "GrassPerTri * InstanceCount.")]
    public int grassPerTri = 4;

    [Tooltip("In my testing this made performance worse. Even though it feels like it should be faster with how indirect instancing works. Which is frustrating because ykno " +
        "I made the feature and then it's just worse somehow but well, here we are. You can try it out anyway and see if it's better for your situation. " +
        "IMPORTANT: You'll need to enable a shader keyword at the top of GrassFlow/Shaders/GrassStructsVars.cginc by uncommenting it for this to work properly.")]
    public bool useIndirectInstancing = false;

    [Tooltip("Does this really need a tooltip? Uhh, well chunk bounds are expanded automatically by blade height to avoid grass popping out when the bounds are culled at strange angles.")]
    [HideInInspector] public bool visualizeChunkBounds = false;

    [Tooltip("Dicards chunks that don't have ANY grass in them based on the parameter map density channel, " +
        "this will be significantly more performant if your terrain has large areas without grass." +
        "WARNING: enabling this removes the chunks completely, meaning that grass could not be dynamically added back in those chunks during runtime. " +
        "Recommended you leave this off while styling the grass or you might remove chunks and then if you try to paint density back into those areas it wont show up until you refresh.")]
    public bool discardEmptyChunks = true;

    RenderTexture terrainNormalMap;
    float terrainMapOffset = 0.0005f;

    [Tooltip("Controls the LOD parameter of the grass. X = render distance. Y = density falloff sharpness (how quickly the amount of grass is reduced to zero). " +
        "Z = offset, basically a positive number prevents blades from popping out within this distance.")]
    [SerializeField] private Vector3 _lodParams = new Vector3(15, 1.1f, 0);
    public Vector3 lodParams {
        get { return _lodParams; }
        set {
            _lodParams = value;
            if (drawMat) drawMat.SetVector("_LOD", value);
        }
    }

    [SerializeField] private float maxRenderDistSqr = 150f * 150f;

    [Tooltip("Controls max render dist of the grass chunks. This value is mostly just used to quickly reject far away chunks for rendering.")]
    [SerializeField] private float _maxRenderDist = 150f;
    public float maxRenderDist {
        get { return _maxRenderDist; }
        set {
            _maxRenderDist = value;
            maxRenderDistSqr = value * value;
        }
    }

    public int chunksX = 5;
    public int chunksY = 1;
    public int chunksZ = 5;

    bool hasRequiredAssets {
        get {
            bool sharedAssets = grassMaterial && terrainTransform;
            if (renderType == GrassRenderType.Mesh) {
                return sharedAssets && grassMesh;
            } else {
                return sharedAssets && terrainObject;
            }
        }
    }

    [SerializeField] [HideInInspector] public Material drawMat;

    [System.NonSerialized] public RenderTexture colorMapRT;
    [System.NonSerialized] public RenderTexture paramMapRT;

    MeshChunker.MeshChunk[] terrainChunks;


    public enum GrassRenderType { Terrain, Mesh }


    //Static Vars
    static ComputeShader gfComputeShader;
    static ComputeBuffer rippleBuffer;
    static ComputeBuffer counterBuffer;
    static int updateRippleKernel;
    static int addRippleKernel;
    static int noiseKernel;
    static int normalKernel;
    static int emptyChunkKernel;
    static int ripDeltaTimeHash = Shader.PropertyToID("ripDeltaTime");

    static RenderTexture noise3DTexture;

    static ComputeShader paintShader;
    static int paintKernel;
    static int splatKernel;

    public static HashSet<GrassFlowRenderer> instances = new HashSet<GrassFlowRenderer>();
    static bool runRipple = true;

    /// <summary>
    /// This is set to true as soon as a ripple is added and stays true unless manually set to false.
    /// When true it signals the ripple update shaders to run, it doesn't take long to run them and theres no easy generic way to know when all ripples are depleted without asking the gpu for the memory which would be slow.
    /// But you can manually set this if you know your ripples only last a certain amount of time or something.
    /// Realistically its not that important though.
    /// </summary>
    public static bool updateRipples = false;



    //-------------Actual code-------------

    void Awake() {
        instances.Add(this);
    }

    private void Start() {
        if (hasRequiredAssets)
            Init();
    }

    public void OnEnable() {
        UpdateTransform();
        Camera.onPreCull -= Render;

        if (hasRequiredAssets) {
            if (!initialized) {
                Init();
            } else {
                Camera.onPreCull += Render;
            }
        }

        //have to reset these on enable due to reasons related to what is described in OnDisable
        CheckIndirectInstancingArgs();
    }



    private void OnDisable() {
        Camera.onPreCull -= Render;

        //unity throws a buncha warnings about the indirect args buffer being unallocated and disposed by the garbage collector when scripts are rebuilt if we dont do this
        //becuse of how unity's weird system of serialization works it just automatically unallocates the buffer on reload so we have to catch it here and do it manually
        //because for whatever reason youre not supposed to let garbage collection dispose of them automatically or itll complain
        ReleaseIndirectArgsBuffers();
    }


#if UNITY_EDITOR

    private void Reset() {
        terrainTransform = transform;
        terrainObject = GetComponent<Terrain>();

        MeshFilter meshF;
        if (meshF = GetComponent<MeshFilter>()) {
            grassMesh = meshF.sharedMesh;
            renderType = GrassRenderType.Mesh;
        }

    }


    //the validation function is mainly to regenerate certain things that are lost upon unity recompiling scripts
    //but also in some other situations like saving the scene
    private void OnValidate() {
        if (!isActiveAndEnabled || !hasRequiredAssets || StackTraceUtility.ExtractStackTrace().Contains("Inspector"))
            return;

        if (terrainChunks == null) {
            Refresh();
        } else {
            GetResources();
            UpdateShaders();
            MapSetup();
        }


        //on script reload property blocks are lost for some reason so re add them so they dont break
        if (renderType == GrassRenderType.Terrain) {
            foreach (MeshChunker.MeshChunk chunk in terrainChunks) {
                if (chunk.propertyBlock == null) {
                    chunk.propertyBlock = new MaterialPropertyBlock();
                    chunk.propertyBlock.SetVector("_chunkPos", chunk.chunkPos);
                }
            }
        }


        Camera.onPreCull -= Render;
        Camera.onPreCull += Render;
    }

    private void OnDrawGizmos() {
        if (!visualizeChunkBounds) return;

        Gizmos.color = Color.green;
        foreach (MeshChunker.MeshChunk chunk in terrainChunks) {
            Gizmos.DrawWireCube(chunk.worldBounds.center, chunk.worldBounds.size);
        }
    }

#endif

    /// <summary>
    /// Releases current assets and reinitializes the grass.
    /// Warning: Will reset current map paint status. (If that is the intended effect, use RevertDetailMaps() instead)
    /// </summary>
    public void Refresh() {
        if (isActiveAndEnabled) {
            ReleaseAssets();

            Init();
        }
    }

    bool initialized = false;
    void Init() {
        if (!hasRequiredAssets) {
            Debug.LogError("GrassFlow: Not all required assets assigned in the inspector!");
            return;
        }

        if (!isActiveAndEnabled) return;

        GetResources();

        CheckRippleBuffers();

        MapSetup();

        HandleLodChunks();

        UpdateShaders();

        UpdateTransform();

        Camera.onPreCull -= Render;
        Camera.onPreCull += Render;
        initialized = true;
    }


    void CheckIndirectInstancingArgs() {
        if (useIndirectInstancing) {
            if (terrainChunks != null) {
                foreach (MeshChunker.MeshChunk chunk in terrainChunks) {
                    if (chunk.indirectArgs == null) {
                        chunk.SetIndirectArgs();
                    }
                }
            }
        } else {
            ReleaseIndirectArgsBuffers();
        }
    }

    void ReleaseIndirectArgsBuffers() {
        if (terrainChunks != null) {
            foreach (MeshChunker.MeshChunk chunk in terrainChunks) {
                if (chunk.indirectArgs != null) {
                    chunk.indirectArgs.Release();
                    chunk.indirectArgs = null;
                }
            }
        }
    }

    void HandleLodChunks() {
        if (terrainChunks != null) {
            ReleaseIndirectArgsBuffers();
        }

        if (renderType == GrassRenderType.Mesh) {
            terrainChunks = MeshChunker.Chunk(grassMesh, chunksX, chunksY, chunksZ, grassPerTri, drawMat.GetFloat("bladeHeight"));
        } else {
            terrainChunks = MeshChunker.ChunkTerrain(terrainObject, chunksX, chunksZ, grassPerTri, terrainExpansion, drawMat.GetFloat("bladeHeight"));
            if (!terrainNormalMap) {
                terrainNormalMap = TextureCreator.GetTerrainNormalMap(terrainObject, gfComputeShader, normalKernel, highQualityHeightmap);
                terrainMapOffset = 1f / terrainNormalMap.width * 0.5f;
            }

            if (discardEmptyChunks) DiscardUnusedChunks();
        }


        CheckIndirectInstancingArgs();
    }

    void DiscardUnusedChunks() {
        Texture paramTex;
        if (!(paramTex = paramMapRT)) paramTex = paramMap;

        if (terrainChunks == null || !hasRequiredAssets || !paramTex 
            || renderType != GrassRenderType.Terrain) return;

        gfComputeShader.SetVector("chunkDims", new Vector4(chunksX, chunksZ));
        gfComputeShader.SetTexture(emptyChunkKernel, "paramMap", paramTex);
        ComputeBuffer chunkResultsBuffer = new ComputeBuffer(terrainChunks.Length, sizeof(int));
        int[] chunkResults = new int[terrainChunks.Length];
        chunkResultsBuffer.SetData(chunkResults);
        gfComputeShader.SetBuffer(emptyChunkKernel, "chunkResults", chunkResultsBuffer);

        gfComputeShader.Dispatch(emptyChunkKernel, Mathf.CeilToInt(paramMap.width / paintThreads), Mathf.CeilToInt(paramMap.height / paintThreads), 1);

        chunkResultsBuffer.GetData(chunkResults);
        chunkResultsBuffer.Release();

        List<MeshChunker.MeshChunk> resultChunks = new List<MeshChunker.MeshChunk>();
        for(int i = 0; i < terrainChunks.Length; i++) {
            if (chunkResults[i] > 0) resultChunks.Add(terrainChunks[i]);
        }

        terrainChunks = resultChunks.ToArray();
    }

    new void Destroy(Object obj) {
        if (Application.isPlaying) {
            Object.Destroy(obj);
        } else {
            DestroyImmediate(obj);
        }
    }


    void ReleaseAssets() {
        Camera.onPreCull -= Render;

        ReleaseDetailMapRTs();

        drawMat = null;

        if (terrainNormalMap) terrainNormalMap.Release();
        terrainNormalMap = null;

        if (terrainChunks != null) {
            foreach (MeshChunker.MeshChunk chunk in terrainChunks) {
                if (chunk.indirectArgs != null) {
                    chunk.indirectArgs.Release();
                    chunk.indirectArgs = null;
                }

                Destroy(chunk.mesh);
            }

            terrainChunks = null;
        }
    }

    void ReleaseDetailMapRTs() {
        if (colorMapRT) colorMapRT.Release(); colorMapRT = null;
        if (paramMapRT) paramMapRT.Release(); paramMapRT = null;
    }

    /// <summary>
    /// Reverts unsaved paints to grass color and paramter maps.
    /// </summary>
    public void RevertDetailMaps() {
        ReleaseDetailMapRTs();
        MapSetup();
    }

    Matrix4x4[] matrices;


    void MakeMatrices(Matrix4x4 tMatrix) {
        matrices = new Matrix4x4[instanceCount];
        for (int i = 0; i < instanceCount; i++) {
            matrices[i] = tMatrix;
        }
    }

    /// <summary>
    /// Updates the transformation matrices used to render grass.
    /// You should call this if the object the grass is attached to moves.
    /// </summary>
    public void UpdateTransform() {
        if (!terrainTransform) return;

        Matrix4x4 tMatrix = terrainTransform.localToWorldMatrix;

        if (useIndirectInstancing) {
            SetDrawmatObjMatrices();
        } else {
            MakeMatrices(tMatrix);
        }

        if (terrainChunks == null) return;
        if (renderType == GrassRenderType.Mesh) {
            foreach (MeshChunker.MeshChunk chunk in terrainChunks) {
                //need to transform the chunk bounds to match the new matrix
                chunk.worldBounds.center = tMatrix.MultiplyPoint(chunk.meshBounds.center);
                chunk.worldBounds.extents = tMatrix.MultiplyVector(chunk.meshBounds.extents);

                if (useManualCulling) {
                    //If we dont do this sometimes the extents could be negative which for some reason breaks the
                    //GeometryUtility.TestPlanesAABB check for manual culling, i love when things like this happen and i 
                    //have to spend ages debugging something that really isnt even my problem :)
                    Vector3 ext = chunk.worldBounds.extents;
                    ext.x = Mathf.Abs(ext.x);
                    ext.y = Mathf.Abs(ext.y);
                    ext.z = Mathf.Abs(ext.z);
                    chunk.worldBounds.extents = ext;
                }
            }
        } else {
            foreach (MeshChunker.MeshChunk chunk in terrainChunks) {
                chunk.worldBounds.center = chunk.meshBounds.center + terrainTransform.position;
            }
        }

    }

    void SetDrawmatObjMatrices() {
        if (drawMat) {
            drawMat.SetMatrix("objToWorldMatrix", terrainTransform.localToWorldMatrix);
            drawMat.SetMatrix("worldToObjMatrix", terrainTransform.worldToLocalMatrix);
        }
    }

    void CheckRippleBuffers() {
        if (rippleBuffer == null) {
            rippleBuffer = new ComputeBuffer(128, Marshal.SizeOf(typeof(RippleData)));
        }
        if (counterBuffer == null) {
            counterBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(Vector4)));
            counterBuffer.SetData(new Vector4[] { Vector4.zero });
        }
    }

    void GetResources() {
        if (!drawMat) {
            drawMat = useMaterialInstance ? Instantiate(grassMaterial) : grassMaterial;
        }


        if (renderType == GrassRenderType.Mesh) {
            drawMat.EnableKeyword("RENDERMODE_MESH");
        } else {
            drawMat.DisableKeyword("RENDERMODE_MESH");
        }

#if UNITY_EDITOR
        drawMat.EnableKeyword("GRASS_EDITOR");
#else
        drawMat.DisableKeyword("GRASS_EDITOR");
#endif



        if (!gfComputeShader) gfComputeShader = Resources.Load<ComputeShader>("GrassFlow/GrassFlowCompute");
        addRippleKernel = gfComputeShader.FindKernel("AddRipple");
        updateRippleKernel = gfComputeShader.FindKernel("UpdateRipples");
        noiseKernel = gfComputeShader.FindKernel("NoiseMain");
        normalKernel = gfComputeShader.FindKernel("NormalsMain");
        emptyChunkKernel = gfComputeShader.FindKernel("EmptyChunkDetect");

        if (!paintShader) paintShader = Resources.Load<ComputeShader>("GrassFlow/GrassFlowPainter");
        paintKernel = paintShader.FindKernel("PaintKernel");
        splatKernel = paintShader.FindKernel("ApplySplatTex");

        if (!noise3DTexture) {
            noise3DTexture = Resources.Load<RenderTexture>("GrassFlow/GF3DNoise");
            noise3DTexture.Release();
            noise3DTexture.enableRandomWrite = true;
            noise3DTexture.Create();

            //compute 3d noise
            gfComputeShader.SetTexture(noiseKernel, "NoiseResult", noise3DTexture);
            gfComputeShader.Dispatch(noiseKernel, noise3DTexture.width / 8, noise3DTexture.height / 8, noise3DTexture.volumeDepth / 8);
        }
    }

    void MapSetup() {
        if (enableMapPainting) {
            CheckMap(colorMap, ref colorMapRT, RenderTextureFormat.ARGBHalf);
            CheckMap(paramMap, ref paramMapRT, RenderTextureFormat.ARGB32);
        }
    }

    void CheckMap(Texture2D srcMap, ref RenderTexture outRT, RenderTextureFormat format) {
        if (srcMap && !outRT) {
            RenderTexture oldRT = RenderTexture.active;
            outRT = new RenderTexture(srcMap.width, srcMap.height, 0, format, RenderTextureReadWrite.Linear) {
                enableRandomWrite = true, filterMode = srcMap.filterMode, wrapMode = srcMap.wrapMode, name = srcMap.name + "RT"
            };
            outRT.Create();
            Graphics.Blit(srcMap, outRT);
            RenderTexture.active = oldRT;
        }
    }

    struct RippleData {
        Vector4 pos; // w = strength
        Vector4 drssParams;//xyzw = decay, radius, sharpness, speed 
    }


    private void Update() {
        UpdateRipples();

#if UNITY_EDITOR
        if (updateBuffers && hasRequiredAssets)
            UpdateShaders();
#endif
    }

    /// <summary>
    /// This basically sets all required variables and textures to the various shaders to make them run.
    /// You might need to call this after changing certain variables/textures to make them take effect.
    /// </summary>
    public void UpdateShaders() {
        if (!drawMat) return;

        if (rippleBuffer != null && counterBuffer != null) {
            drawMat.SetBuffer("rippleBuffer", rippleBuffer);
            drawMat.SetBuffer("rippleCount", counterBuffer);

            try {
                gfComputeShader.SetBuffer(addRippleKernel, "rippleBuffer", rippleBuffer);
                gfComputeShader.SetBuffer(updateRippleKernel, "rippleBuffer", rippleBuffer);
                gfComputeShader.SetBuffer(addRippleKernel, "rippleCount", counterBuffer);
                gfComputeShader.SetBuffer(updateRippleKernel, "rippleCount", counterBuffer);
            } catch { }
        }

        if (noise3DTexture) drawMat.SetTexture("_NoiseTex", noise3DTexture);

        if (enableMapPainting) {
            if (colorMapRT) drawMat.SetTexture("colorMap", colorMapRT);
            if (paramMapRT) drawMat.SetTexture("dhfParamMap", paramMapRT);
        } else {
            if (colorMap) drawMat.SetTexture("colorMap", colorMap);
            if (paramMap) drawMat.SetTexture("dhfParamMap", paramMap);
        }

        if (terrainObject) {
            if (terrainNormalMap) drawMat.SetTexture("terrainNormalMap", terrainNormalMap);
            Vector3 terrainScale = terrainObject.terrainData.size;
            drawMat.SetVector("terrainSize", terrainObject.terrainData.size);
            drawMat.SetVector("terrainChunkSize", new Vector4(terrainScale.x / chunksX, terrainScale.z / chunksZ));
            drawMat.SetFloat("terrainExpansion", terrainExpansion);
            drawMat.SetFloat("terrainMapOffset", terrainMapOffset);
        }

        //a bit weird but saves having to do an extra division in the shader ¯\_(ツ)_/¯
        drawMat.SetFloat("numTexturesPctUV", 1.0f / drawMat.GetFloat("numTextures"));

        if (useIndirectInstancing && terrainTransform) {
            SetDrawmatObjMatrices();
        }
    }


    //----------------------------------
    //MAIN RENDER FUNCTION--------------
    //----------------------------------
    Plane[] frustumPlanes;
    void Render(Camera cam) {
#if UNITY_EDITOR
        //these arent really as much of an issue in a built game
        if (cam.name == "Preview Scene Camera") return;
        if (!hasRequiredAssets) OnDisable();
#endif

        if (useManualCulling)
            frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);

        float instanceMult = instanceCount;

        foreach (MeshChunker.MeshChunk chunk in terrainChunks) {
            if (useManualCulling && !GeometryUtility.TestPlanesAABB(frustumPlanes, chunk.worldBounds)) {
                continue;
            }

            float camDist = chunk.worldBounds.SqrDistance(cam.transform.position);
            if (camDist > maxRenderDistSqr) {
                continue;
            }

            camDist = Mathf.Sqrt(camDist) - lodParams.z;
            if (camDist <= 0f) camDist = 0.0001f;
            camDist = 1.0f / camDist;

            float bladeCnt = Mathf.Pow(camDist * lodParams.x, lodParams.y);
            if (bladeCnt > 1f) bladeCnt = 1f;
            int instCount = (int)(bladeCnt * instanceMult);

            if (instCount > 0) {
                if (useIndirectInstancing) {
                    chunk.instanceCount = (uint)instCount;
                    Graphics.DrawMeshInstancedIndirect(chunk.mesh, 0, drawMat, chunk.worldBounds, chunk.indirectArgs, 0, chunk.propertyBlock, shadowMode, receiveShadows, renderLayer, cam);
                } else {
                    Graphics.DrawMeshInstanced(chunk.mesh, 0, drawMat, matrices, instCount, chunk.propertyBlock, shadowMode, receiveShadows, renderLayer, cam);
                }
            }
        }
    }


    //--------------------------------    
    //RIPPLES-------------------------
    //--------------------------------
    private void LateUpdate() {
        runRipple = true;
    }

    void UpdateRipples() {
        if (runRipple && updateRipples) {
            runRipple = false;
            gfComputeShader.SetFloat(ripDeltaTimeHash, Time.deltaTime);
            gfComputeShader.Dispatch(updateRippleKernel, 1, 1, 1);
        }
    }


    /// <summary>
    /// Adds a ripple into the ripple buffer that affects all grasses.
    /// Ripples are just that, ripples that animate accross the grass, a simple visual effect.
    /// </summary>
    /// <param name="pos">World position the ripple is placed at.</param>
    /// <param name="strength">How forceful the ripple is.</param>
    /// <param name="decayRate">How quickly the ripple dissipates.</param>
    /// <param name="speed">How fast the ripple moves across the grass.</param>
    /// <param name="startRadius">Start size of the ripple.</param>
    /// <param name="sharpness">How much this ripple appears like a ring rather than a circle.</param>
    public static void AddRipple(Vector3 pos, float strength = 1f, float decayRate = 2.5f, float speed = 25f, float startRadius = 0f, float sharpness = 0f) {
        if (!gfComputeShader) return;

        gfComputeShader.SetVector("pos", new Vector4(pos.x, pos.y, pos.z, strength));
        gfComputeShader.SetVector("drssParams", new Vector4(decayRate, startRadius, sharpness, speed));
        gfComputeShader.Dispatch(addRippleKernel, 1, 1, 1);
        updateRipples = true;
    }

    /// <summary>
    /// Adds a ripple into the ripple buffer that affects all grasses.
    /// Ripples are just that, ripples that animate accross the grass, a simple visual effect.
    /// </summary>
    /// <param name="pos">World position the ripple is placed at.</param>
    /// <param name="strength">How forceful the ripple is.</param>
    /// <param name="decayRate">How quickly the ripple dissipates.</param>
    /// <param name="speed">How fast the ripple moves across the grass.</param>
    /// <param name="startRadius">Start size of the ripple.</param>
    /// <param name="sharpness">How much this ripple appears like a ring rather than a circle.</param>
    public void AddARipple(Vector3 pos, float strength = 1f, float decayRate = 2.5f, float speed = 25f, float startRadius = 0f, float sharpness = 0f) {
        AddRipple(pos, strength, decayRate, speed, startRadius, sharpness);
    }


    //--------------------------------    
    //PAINTING------------------------
    //--------------------------------    
    static int mapToPaintID = Shader.PropertyToID("mapToPaint");
    const float paintThreads = 8f;

    /// <summary>
    /// Sets the texture to be used when calling paint functions.
    /// </summary>
    public static void SetPaintBrushTexture(Texture2D brushTex) {
        if (paintShader)
            paintShader.SetTexture(paintKernel, "brushTexture", brushTex);
    }

    /// <summary>
    /// Paints color onto the colormap.
    /// enableMapPainting needs to be turned on for this to work.
    /// Uses a global texture as the brush texture, should be set via SetPaintBrushTexture(Texture2D brushTex).
    /// </summary>
    /// <param name="texCoord">texCoord to paint at, usually obtained by a raycast.</param>
    /// <param name="clampRange">Clamp the painted values between this range. Not really used for colors but exists just in case.
    /// Should be set to 0 to 1 or greater than 1 for HDR colors.</param>
    public void PaintColor(Vector2 texCoord, float brushSize, float brushStrength, Color colorToPaint, Vector2 clampRange) {
        PaintDispatch(texCoord, brushSize, brushStrength, colorToPaint, colorMapRT, clampRange, 0f);
    }

    /// <summary>
    /// Paints parameters onto the paramMap.
    /// enableMapPainting needs to be turned on for this to work.
    /// Uses a global texture as the brush texture, should be set via SetPaintBrushTexture(Texture2D brushTex).
    /// </summary>
    /// <param name="texCoord">texCoord to paint at, usually obtained by a raycast.</param>
    /// <param name="densityAmnt">Amount density to paint.</param>
    /// <param name="heightAmnt">Amount height to paint.</param>
    /// <param name="flattenAmnt">Amount flatten to paint.</param>
    /// <param name="windAmnt">Amount wind to paint.</param>
    /// <param name="clampRange">Clamp the painted values between this range. Valid range for parameters is 0 to 1.</param>
    public void PaintParameters(Vector2 texCoord, float brushSize, float brushStrength, float densityAmnt, float heightAmnt, float flattenAmnt, float windAmnt, Vector2 clampRange) {
        PaintDispatch(texCoord, brushSize, brushStrength, new Vector4(densityAmnt, heightAmnt, flattenAmnt, windAmnt), paramMapRT, clampRange, 1f);
    }

    /// <summary>
    /// A more manual paint function that you most likely don't want to use.
    /// It's mostly only exposed so that the GrassFlowInspector can use it. But maybe you want to too, I'm not the boss of you.
    /// You could use this to paint onto your own RenderTextures.
    /// </summary>
    /// <param name="blendMode">Controls blend type: 0 for lerp towards, 1 for additive</param>
    public static void PaintDispatch(Vector2 texCoord, float brushSize, float brushStrength, Vector4 blendParams, RenderTexture mapRT, Vector2 clampRange, float blendMode) {
        if (!paintShader || !mapRT) return;

        //srsBrushParams = (strength, radius, unused, alpha controls type/ 0 for lerp towards, 1 for additive)
        paintShader.SetVector(srsBrushParamsID, new Vector4(brushStrength, brushSize * 0.05f, 0, blendMode));
        paintShader.SetVector(clampRangeID, clampRange);

        paintShader.SetVector(brushPosID, texCoord);
        paintShader.SetTexture(paintKernel, mapToPaintID, mapRT);
        paintShader.SetVector(blendParamsID, blendParams);

        paintShader.Dispatch(paintKernel, Mathf.CeilToInt(mapRT.width / paintThreads), Mathf.CeilToInt(mapRT.height / paintThreads), 1);
    }


    /// <summary>
    /// Automatically controls grass density based on a splat layer from terrain data.
    /// </summary>
    /// <param name="terrain">Terrain to get splat data from</param>
    /// <param name="splatLayer">Zero based index of the splat layer from the terrain to use.</param>
    /// <param name="mode">Controls how the tex is applied. 0 = additive, 1 = subtractive, 2 = replace.</param>
    /// <param name="splatTolerance">Controls opacity tolerance.</param>
    public void ApplySplatTex(Terrain terrain, int splatLayer, int mode, float splatTolerance) {
        int channel = splatLayer % 4;
        int texIdx = splatLayer / 4;

        ApplySplatTex(terrain.terrainData.alphamapTextures[texIdx], channel, mode, splatTolerance);
    }

    /// <summary>
    /// Automatically controls grass density based on a splat tex.
    /// </summary>
    /// <param name="splatAlphaMap">The particular splat alpha map texture that has the desired splat layer on it.</param>
    /// <param name="channel">Zero based index of the channel of the texture that represents the splat layer.</param>
    /// <param name="mode">Controls how the tex is applied. 0 = additive, 1 = subtractive, 2 = replace.</param>
    /// <param name="splatTolerance">Controls opacity tolerance.</param>
    public void ApplySplatTex(Texture2D splatAlphaMap, int channel, int mode, float splatTolerance) {
        if (!enableMapPainting || !paramMapRT) {
            Debug.LogError("Couldn't apply splat tex, map painting not enabled!");
            return;
        }

        paintShader.SetTexture(splatKernel, "splatTex", splatAlphaMap);
        paintShader.SetTexture(splatKernel, "mapToPaint", paramMapRT);

        paintShader.SetInt("splatMode", mode);
        paintShader.SetInt("splatChannel", channel);

        paintShader.SetFloat("splatTolerance", splatTolerance);

        paintShader.Dispatch(splatKernel, Mathf.CeilToInt(paramMapRT.width / paintThreads), Mathf.CeilToInt(paramMapRT.width / paintThreads), 1);
    }

    static int srsBrushParamsID = Shader.PropertyToID("srsBrushParams");
    static int clampRangeID = Shader.PropertyToID("clampRange");
    static int brushPosID = Shader.PropertyToID("brushPos");
    static int blendParamsID = Shader.PropertyToID("blendParams");


    private void OnDestroy() {
        ReleaseAssets();

        if (rippleBuffer != null) rippleBuffer.Release();
        if (counterBuffer != null) counterBuffer.Release();
    }
}
