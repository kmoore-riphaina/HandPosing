using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PoseAuthoring.HandAnimation.VectorGraph;
using UnityEditor;
using Zenject;

public partial class AnimationVectorAnalyser : MonoBehaviour
{
    // zaxis = time
    // yaxis = value
    // xaxis 
    private IVectorGraphService _lineEngine;

    [Inject]
    public void Construct(IVectorGraphService vectorGraphService)
    {
        _lineEngine = vectorGraphService;
    }
    //private List<GameObject> lines;
    // Start is called before the first frame update
    void Start()
    {
        //lines = new List<GameObject>();

        List<Dictionary<int, VectorAnalysisPoint>> vac = new List<Dictionary<int, VectorAnalysisPoint>>();

        var dict = new Dictionary<int, VectorAnalysisPoint>();
        dict.Add(0, new VectorAnalysisPoint { index = 0, XvectorName = "thumb1", YTotalDelta = 0, ZTimeReference = 0 });
        dict.Add(1, new VectorAnalysisPoint { index = 1, XvectorName = "thumb1", YTotalDelta = 2, ZTimeReference = 1 });
        dict.Add(2, new VectorAnalysisPoint { index = 1, XvectorName = "thumb1", YTotalDelta = 1, ZTimeReference = 2 });
        dict.Add(3, new VectorAnalysisPoint { index = 1, XvectorName = "thumb1", YTotalDelta = 0, ZTimeReference = 3 });
        dict.Add(4, new VectorAnalysisPoint { index = 1, XvectorName = "thumb1", YTotalDelta = 1, ZTimeReference = 4 });
        var th2 = new Dictionary<int, VectorAnalysisPoint>();
        th2.Add(0, new VectorAnalysisPoint { index = 0, XvectorName = "thumb2", YTotalDelta = 0, ZTimeReference = 0 });
        th2.Add(1, new VectorAnalysisPoint { index = 1, XvectorName = "thumb2", YTotalDelta = 2, ZTimeReference = 1 });
        th2.Add(2, new VectorAnalysisPoint { index = 1, XvectorName = "thumb2", YTotalDelta = 1, ZTimeReference = 2 });
        th2.Add(3, new VectorAnalysisPoint { index = 1, XvectorName = "thumb2", YTotalDelta = 0, ZTimeReference = 3 });
        th2.Add(4, new VectorAnalysisPoint { index = 1, XvectorName = "thumb2", YTotalDelta = 1, ZTimeReference = 4 });
        vac.Add(dict);
        vac.Add(th2);
        GraphVectorsNew(vac);
        //var vectors = findBoundingBox();
        //var start = vectors[0];
        //var end = vectors[1];
        //GameObject myLine = new GameObject();
        //LineRenderer lr = myLine.AddComponent<LineRenderer>();
        //lr.startWidth = 0.01f;
        //lr.endWidth = 0.01f;
        //lr.SetPosition(0, start);
        //lr.SetPosition(1, end);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private Vector3[] findBoundingBox()
    {
        var render = this.gameObject.GetComponent<Renderer>();
        Vector3[] boundingBox = new Vector3[2];
        boundingBox[0] = render.bounds.min;
        boundingBox[1] = render.bounds.max;
        return boundingBox;
    }

    private Dictionary<string, int> vertexNametoindexmap = new Dictionary<string, int>();
    private Dictionary<int, bool> timechange = new Dictionary<int, bool>();


    //public void GraphVectors(List<Dictionary<int, VectorAnalysisPoint>> vectorAnalysisPointsCollection)
    //{
    //    var render = this.gameObject.GetComponent<Renderer>();
    //    var startPos = render.bounds.min;
    //    foreach (var v in vectorAnalysisPointsCollection)
    //    {
    //        for (int i = 0; i < v.Count - 1; i++)
    //        {
    //            float xAxisReference;
    //            float x, y, z;
    //            GetVector(startPos, v, i, out xAxisReference, out x, out y, out z);

    //            GameObject myLine = new GameObject();

    //            myLine.transform.position = startPos;
    //            LineRenderer lr = myLine.AddComponent<LineRenderer>();
    //            lr.startWidth = 0.01f;
    //            lr.endWidth = 0.01f;
    //            lr.SetPosition(0, new Vector3(x, y, z));
    //            float x1, y1, z1;
    //            GetVector(startPos, v, i + 1, out xAxisReference, out x1, out y1, out z1);
    //            lr.SetPosition(1, new Vector3(x1, y1, z1));
    //        }
    //    }
    //}
    public void GraphVectorsNew(List<Dictionary<int, VectorAnalysisPoint>> vectorAnalysisPointsCollection)
    {
        var render = this.gameObject.GetComponent<Renderer>();
        var startPos = render.bounds.min;
        SetXaxisSegmentationMultiplyer(render, vectorAnalysisPointsCollection.Count);
        foreach (var v in vectorAnalysisPointsCollection)
        {
            float? lastZ = null;
            for (int i = 0; i < v.Count; i++)
            {
                var vector = GetVector(startPos, v, i);
                if(lastZ== null || lastZ != vector.Value.z)
                {
                    lastZ = vector.Value.z;
                }
                if (vector != null)
                {
                    _lineEngine.AddLine(v[i].XvectorName, i, (Vector3)vector, startPos, this.transform);
                }
            }
        }
    }

    private void SetXaxisSegmentationMultiplyer(Renderer render, int pointcount)
    {
        _XSegmentationMultiplyer = (render.bounds.max.x - render.bounds.min.x) / pointcount;
    }

    private float _XSegmentationMultiplyer;
    private Vector3? GetVector(Vector3 startVector, Dictionary<int, VectorAnalysisPoint> v, int i)
    {
        if (!vertexNametoindexmap.ContainsKey(v[i].XvectorName))
        {
            vertexNametoindexmap.Add(v[i].XvectorName, vertexNametoindexmap.Count);
        }
        float xAxisReference = vertexNametoindexmap[v[i].XvectorName];
        float xm = (xAxisReference * _XSegmentationMultiplyer);
        float ym = (v[i].YTotalDelta * 0.1f);
        float zm = (v[i].ZTimeReference );
        Debug.Log(string.Format("xm = {0}; ym = {1}, zm = {2}", xm, ym, zm));
        return new Vector3(startVector.x + xm, startVector.y + ym, startVector.z + zm);
    }
}
