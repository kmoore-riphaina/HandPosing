using System;
using System.Collections.Generic;
using UnityEngine;

public partial class AnimationVectorAnalyser
{
    public class LineEngine : IVectorGraphService
    {
        private Dictionary<string, GameObject> _lines;

        public LineEngine()
        {
            if (_lines == null)
            {
                _lines = new Dictionary<string, GameObject>();
            }
        }
        public void AddLine(string handPointReference, int i, Vector3 vectorPoint, Vector3 startPoint, Transform parent)
        {
            if (!_lines.ContainsKey(handPointReference))
            {
                GameObject myLine = new GameObject();
                myLine.name = handPointReference;
                myLine.transform.parent = parent;
                //myLine.transform.position = new Vector3(startPoint.x, startPoint.y, startPoint.z);
                LineRenderer lr = myLine.AddComponent<LineRenderer>();
                lr.startWidth = 0.025f;
                lr.endWidth = 0.025f;
                lr.SetPosition(0, vectorPoint);
                _lines[handPointReference] = myLine;
            }
            else
            {

                var lr = _lines[handPointReference].GetComponent<LineRenderer>();
                if (lr.positionCount != i + 1)
                {
                    lr.positionCount++;
                }
                lr.SetPosition(i, vectorPoint);
            }
        }
    }
}
