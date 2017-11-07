using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorSurface : MonoBehaviour
{
    #region CLASS VARIABLES

    public static int numRecursive = 1;
    public static int RecursionMaxDepth = 4; 
    public static int TextureSize = 512; 
    public static int LayerID = 5; 
    public static List<MirrorSurface> AllSurfaces = new List<MirrorSurface>();

    #endregion

    private Camera RecursiveCam;
    public bool isParallelOnly = true;

    private RenderTexture m_ReflectionTexture;
    public Color backgroundColor = Color.cyan;
    public Vector3 ReflectionNormal = Vector3.up;

    private RenderTexture FinalRenderTexture;

    [HideInInspector]
    public Vector3 worldNormal;
    [HideInInspector]
    public Bounds bounds;
    private float m_ClipPlaneOffset = 0.07f;

    private bool isDebug = false;
    private float angleLimit = -0.1f; 

    private Material mat;

    void Start()
    {
        m_ReflectionTexture = new RenderTexture(1, 1, 1);
        FinalRenderTexture = new RenderTexture(1, 1, 1);
        bounds = GetComponent<Renderer>().bounds;
        CreateCamera();
        AllSurfaces.Add(this);
        numRecursive++;
        mat = GetComponent<Renderer>().material;


        gameObject.layer = LayerID; 
    }

    public void Reset()
    {
        if (m_ReflectionTexture != FinalRenderTexture)
        {
            m_ReflectionTexture.Release();
        }
        else
        {
            m_ReflectionTexture = new RenderTexture(1, 1, 1);
        }
    }

    void CreateCamera()
    {
        GameObject go = new GameObject("Mirror Refl Camera id" + GetInstanceID(), typeof(Camera), typeof(Skybox));
        RecursiveCam = go.GetComponent<Camera>(); ;
        RecursiveCam.enabled = false;
        go.transform.parent = this.transform;
        RecursiveCam.transform.position = transform.position;
        RecursiveCam.transform.rotation = transform.rotation;
        RecursiveCam.targetDisplay = numRecursive;
        RecursiveCam.targetTexture = m_ReflectionTexture;
        RecursiveCam.renderingPath = RenderingPath.DeferredShading;
        RecursiveCam.clearFlags = CameraClearFlags.Color;
        RecursiveCam.backgroundColor = backgroundColor;
    }

    void Update()
    {
        worldNormal = gameObject.transform.TransformVector(ReflectionNormal).normalized;
    }

    private void OnWillRenderObject()
    {
        if (mat.HasProperty("_ReflectionTex"))
        {
            if (Camera.current == Camera.main)
            {
                mat.SetTexture("_ReflectionTex", FinalRenderTexture);
            }
            else
            {
                mat.SetTexture("_ReflectionTex", m_ReflectionTexture);
            }
        }
    }

    public void RecursiveRender(Camera callee, int currDepth, bool isMainCam)
    {
        Vector3 origPos = new Vector3(callee.transform.position.x, callee.transform.position.y, callee.transform.position.z);
        Vector3 origForward = callee.gameObject.transform.forward;
        Matrix4x4 origMatrix = callee.worldToCameraMatrix;
        Matrix4x4 projMatrix = callee.projectionMatrix;

        if (isDebug)
        {
            print(GetInstanceID() + " is starting recursive render");
            print(callee.name + " is caller");
        }

        UpdateCameraModes(callee, RecursiveCam);
        OrientCamera(origPos, origForward, origMatrix, projMatrix);


        if (currDepth >= MirrorSurface.RecursionMaxDepth)
        {
            RenderMirror(callee, currDepth, isMainCam, true);
            return;
        }


        if (isParallelOnly)
        {
            RenderParallelMirrors(currDepth);
        }
        else
        {
            RenderAllVisibleMirrorsSorted(currDepth);
        }

        OrientCamera(origPos, origForward, origMatrix, projMatrix); 
        RenderMirror(callee, currDepth, isMainCam, false);
    }

    private void RenderAllVisibleMirrors(int currDepth)
    {
        for (int i = 0; i < MirrorSurface.AllSurfaces.Count; i++)
        {
            MirrorSurface surf = MirrorSurface.AllSurfaces[i];
            if (this != surf)
            {
                if (isSurfaceVisible(surf))
                {
                    surf.RecursiveRender(RecursiveCam, currDepth + 1, false);
                }
            }
        }
    }

    private void RenderAllVisibleMirrorsSorted(int currDepth)
    {
        float mostFacingAngle = 1;
        MirrorSurface mostSurf = null;
        for (int i = 0; i < MirrorSurface.AllSurfaces.Count; i++)
        {
            MirrorSurface surf = MirrorSurface.AllSurfaces[i];
            if (this != surf && isSurfaceVisible(surf))
            {
                float angle = Vector3.Dot(surf.worldNormal, this.worldNormal);
                if (angle < mostFacingAngle)
                {
                    mostSurf = surf;
                    mostFacingAngle = angle;
                }
            }
        }

        for (int i = 0; i < MirrorSurface.AllSurfaces.Count; i++)
        {
            MirrorSurface surf = MirrorSurface.AllSurfaces[i];
            if (this != surf && isSurfaceVisible(surf) && surf != mostSurf)
            {
                surf.RecursiveRender(RecursiveCam, currDepth + 1, false);
            }
        }
        if (mostSurf != null)
        {
            mostSurf.RecursiveRender(RecursiveCam, currDepth + 1, false);
        }
    }

    private void RenderParallelMirrors(int currDepth)
    {
        MirrorSurface mostFacingMirror = null;
        MirrorSurface mostFacingCamera = null;
        float mostFacingMirrorAngle = 1;
        float mostFacingMainCamAngle = 1;
        for (int i = 0; i < MirrorSurface.AllSurfaces.Count; i++)
        {
            MirrorSurface surf = MirrorSurface.AllSurfaces[i];
            if (this != surf)
            {
                if (isSurfaceVisible(surf))
                {

                    float surfAngle = Vector3.Dot(surf.worldNormal, this.worldNormal);
                    float camAngle = Vector3.Dot(surf.worldNormal, Camera.main.transform.forward); // prioritizing camera normal to limit jumps

                    if (!(isParallelOnly && (angleLimit < surfAngle))) // NEEDS MORE TESTING
                    {
                        if (camAngle < mostFacingMainCamAngle)
                        {
                            mostFacingMainCamAngle = camAngle;
                            mostFacingCamera = surf;
                        }
                        if (surfAngle < mostFacingMirrorAngle)
                        {
                            mostFacingMirrorAngle = surfAngle;
                            mostFacingMirror = surf;
                        }
                    }
                }
            }
        }
        if (isDebug)
        {
            if (mostFacingCamera != null) { print("most facing camera: " + mostFacingCamera.GetInstanceID()); } else print("No facing cam");
            if (mostFacingMirror != null) { print("most facing mirror: " + mostFacingMirror.GetInstanceID()); } else print("no mirror cam"); ;
        }

        if (mostFacingMirror != null)
        {
            mostFacingMirror.RecursiveRender(RecursiveCam, currDepth + 1, false);
        }
    }

    private void RenderMirror(Camera callee, int currDepth, bool isMainCam, bool isMaxDepth)
    {

        if (isMainCam)
        {
            FinalRenderTexture.Release();
            FinalRenderTexture = new RenderTexture(TextureSize, TextureSize, 1);
            RecursiveCam.targetTexture = FinalRenderTexture;
            m_ReflectionTexture = FinalRenderTexture;
        }
        else
        {
            if (m_ReflectionTexture != FinalRenderTexture) m_ReflectionTexture.Release();
            RenderTexture rt = new RenderTexture(TextureSize, TextureSize, 1);
            RecursiveCam.targetTexture = rt;
            m_ReflectionTexture = rt;
        }

        if (isDebug)
        {
            print(GetInstanceID() + " rendering at depth: " + currDepth);
        }

        float reflectionAngle = Vector3.Dot(Camera.main.transform.forward, worldNormal);

        bool invertCulling = reflectionAngle > 0 ? false : true;
        GL.invertCulling = invertCulling;

        mat.SetFloat("_Alpha", 0);

        RecursiveCam.Render();

        mat.SetFloat("_Alpha", 1);
        GL.invertCulling = false;
    }



    bool isSurfaceVisible(MirrorSurface surf)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(RecursiveCam);
        return (GeometryUtility.TestPlanesAABB(planes, surf.bounds));
    }

    private void OnDestroy()
    {
        numRecursive--;
        AllSurfaces.Remove(this);
    }


    private void OrientCamera(Vector3 camPos, Vector3 camForward, Matrix4x4 worldToCam, Matrix4x4 projMatrix)
    {
        Vector3 pos = transform.position;
        Vector3 normal = worldNormal.normalized;


        Vector3 reflectionVector = (camForward - 2 * (Vector3.Dot(camForward, normal) * normal)).normalized;
        float d = -Vector3.Dot(normal, pos);

        Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

        Matrix4x4 reflection = Matrix4x4.zero;
        CalculateReflectionMatrix(ref reflection, reflectionPlane);
        Vector3 oldpos = camPos;
        Vector3 newpos = reflection.MultiplyPoint(oldpos);
        RecursiveCam.worldToCameraMatrix = worldToCam * reflection;
        Matrix4x4 projection = projMatrix;
        RecursiveCam.projectionMatrix = projection;


        RecursiveCam.transform.position = newpos;

    }


    private void UpdateCameraModes(Camera src, Camera dest)
    {
        if (dest == null)
            return;

        dest.clearFlags = src.clearFlags;

        if (src.clearFlags == CameraClearFlags.Skybox)
        {
            Skybox sky = src.GetComponent(typeof(Skybox)) as Skybox;
            Skybox mysky = dest.GetComponent(typeof(Skybox)) as Skybox;
            if (!sky || !sky.material)
            {
                mysky.enabled = false;
            }
            else
            {
                mysky.enabled = true;
                mysky.material = sky.material;
            }
        }

        dest.farClipPlane = src.farClipPlane;
        dest.nearClipPlane = src.nearClipPlane;
        dest.orthographic = src.orthographic;
        dest.fieldOfView = src.fieldOfView;
        dest.aspect = src.aspect;
        dest.orthographicSize = src.orthographicSize;
    }

    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * m_ClipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
        reflectionMat.m01 = (-2F * plane[0] * plane[1]);
        reflectionMat.m02 = (-2F * plane[0] * plane[2]);
        reflectionMat.m03 = (-2F * plane[3] * plane[0]);

        reflectionMat.m10 = (-2F * plane[1] * plane[0]);
        reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
        reflectionMat.m12 = (-2F * plane[1] * plane[2]);
        reflectionMat.m13 = (-2F * plane[3] * plane[1]);

        reflectionMat.m20 = (-2F * plane[2] * plane[0]);
        reflectionMat.m21 = (-2F * plane[2] * plane[1]);
        reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
        reflectionMat.m23 = (-2F * plane[3] * plane[2]);

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;
    }
}
