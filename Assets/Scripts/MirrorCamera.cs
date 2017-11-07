using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Camera))]
public class MirrorCamera : MonoBehaviour
{

    public MirrorSurface[] surfaces = new MirrorSurface[0];
    public Camera cam;


    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        CheckRecursiveObjectsInView();
    }

    public void CheckRecursiveObjectsInView()
    {
        float mostFacingAngle = 1;
        MirrorSurface mostSurf = null;
        for (int i = 0; i < MirrorSurface.AllSurfaces.Count; i++)
        {
            MirrorSurface surf = MirrorSurface.AllSurfaces[i];
            surf.Reset();
            float angle = Vector3.Dot(transform.forward, surf.worldNormal);
            if (angle < mostFacingAngle)
            {
                mostSurf = surf;
                mostFacingAngle = angle;
            }
        }

        for (int i = 0; i < MirrorSurface.AllSurfaces.Count; i++)
        {
            MirrorSurface surf = MirrorSurface.AllSurfaces[i];
            if (isSurfaceVisible(surf) &&
                Vector3.Dot(gameObject.transform.forward, surf.worldNormal) < 0
                && surf != mostSurf)
            {
                surf.RecursiveRender(cam, 0, true);
            }
        }

        if (mostSurf != null)
        {
            mostSurf.RecursiveRender(cam, 0, true);
        }
    }

    private void ResetRecursiveSurfaces()
    {
        for (int i = 0; i < MirrorSurface.AllSurfaces.Count; i++)
        {
            MirrorSurface surf = MirrorSurface.AllSurfaces[i];
            surf.Reset();
        }
    }

    bool isSurfaceVisible(MirrorSurface surf)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return (GeometryUtility.TestPlanesAABB(planes, surf.bounds));
    }
}
