using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;

public class HandAnimator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Animation anim = GetComponent<Animation>();

        // create a new AnimationClip
        AnimationClip clip = new AnimationClip
        {
            legacy = true
        };
        //var prefabCurve = PrefabClip.g
        // get all transforms in object tree
        var transforms = GetComponentsInChildren<Transform>();
        // create a curve to move the GameObject and assign to the clip
        var fingeres = transforms.Where(x => !x.name.ToLower().Contains("ignore") && (x.name.ToLower().Contains("0")
         || x.name.ToLower().Contains("1")
         || x.name.ToLower().Contains("2")
         || x.name.ToLower().Contains("3"))).ToList();


        // target thumb1
        var target = fingeres.First(x => x.name.ToLower().Contains("thumb1"));
        BuildClipFromTransformTarget(clip, target);

        // example for moving position
        //clip.SetCurve("", typeof(Transform), "localPosition.x", curve);

        // SaveAsset
        //AssetDatabase.CreateAsset(clip, "Assets/HandAnimations/HandAnimateThumbDownTest2.anim");
        //AssetDatabase.SaveAssets();

        // now animate the GameObject
        //anim.AddClip(clip, clip.name);
        //anim.Play(clip.name);
    }

    private void BuildClipFromTransformTarget(AnimationClip clip, Transform target)
    {
        var rotationVectors = target.localEulerAngles;

        Keyframe[] yKeys;
        yKeys = new Keyframe[3];
        yKeys[0] = new Keyframe(0.0f, rotationVectors.y);
        yKeys[1] = new Keyframe(1.0f, rotationVectors.y - 20f);
        yKeys[2] = new Keyframe(2.0f, rotationVectors.y);

        AnimationCurve yCurve = new AnimationCurve(yKeys);
        clip.SetCurve(GetRelativePath(this.transform, target), typeof(Transform), "localEulerAnglesRaw.y", yCurve);

        Keyframe[] xKeys;
        xKeys = new Keyframe[3];
        xKeys[0] = new Keyframe(0.0f, rotationVectors.x);
        xKeys[1] = new Keyframe(1.0f, rotationVectors.x - 20f);
        xKeys[2] = new Keyframe(2.0f, rotationVectors.x);

        AnimationCurve xCurve = new AnimationCurve(xKeys);
        clip.SetCurve(GetRelativePath(this.transform, target), typeof(Transform), "localEulerAnglesRaw.x", xCurve);

        Keyframe[] zKeys;
        zKeys = new Keyframe[3];
        zKeys[0] = new Keyframe(0.0f, rotationVectors.z);
        zKeys[1] = new Keyframe(1.0f, rotationVectors.z - 20f);
        zKeys[2] = new Keyframe(2.0f, rotationVectors.z);

        AnimationCurve zCurve = new AnimationCurve(zKeys);
        clip.SetCurve(GetRelativePath(this.transform, target), typeof(Transform), "localEulerAnglesRaw.z", zCurve);
    }

    string GetRelativePath(Transform root, Transform tr)
    {
        List<string> pths = new List<string>();
        Transform t = tr;
        while (t != root && t != t.root)
        {
            pths.Add(t.name);
            t = t.parent;
        }

        pths.Reverse();
        return string.Join("/", pths);
    }
    // Update is called once per frame
    void Update()
    {

    }
}
