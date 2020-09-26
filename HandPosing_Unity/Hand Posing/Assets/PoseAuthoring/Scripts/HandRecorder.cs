using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;
using static OVRPlugin;
using static OVRSkeleton;
using BoneId = OVRSkeleton.BoneId;
using System;
using System.Collections.ObjectModel;

public class HandRecorder : MonoBehaviour, OVRSkeleton.IOVRSkeletonDataProvider
{
    public Stopwatch stopwatch;
    public ArrayList TransformsHolder;
    public GameObject ReplayHand;

    private static HandRecorder instance = null;
    private OVRSkeleton skeleton;

    bool SetDelta = false;
    bool IsDataValid;
    [SerializeField]
    private Hand HandType = Hand.None;
    [SerializeField]
    private GameObject _pointerPoseGO;
    private OVRPlugin.HandState _handState = new OVRPlugin.HandState();
    [SerializeField]
    private IOVRSkeletonDataProvider _dataProvider;
    private readonly Quaternion wristFixupRotation = new Quaternion(0.0f, 1.0f, 0.0f, 0.0f);
    Dictionary<string, Transform> replayTransform = null;
    private IList<OVRBone> _bones = null;
    Dictionary<string, AnglesStore> InitialVectors = null;
    Dictionary<string, AnglesStore> OffsetDelta = null;
    public IList<OVRBone> Bones { get; protected set; }

    public struct AnglesStore
    {
        public AnglesStore(float x, float y, float z, float w)
        {
            _x = x;
            _y = y;
            _z = z;
            _w = w;
        }

        public AnglesStore(Quaternion q) : this()
        {
            _x = q.x;
            _y = q.y;
            _z = q.z;
            _w = q.w;
        }

        public AnglesStore(Vector3 vector3) : this()
        {
            var q = Quaternion.Euler(vector3.x, vector3.y, vector3.z);
            _x = q.x;
            _y = q.y;
            _z = q.z;
            _w = q.w;
        }

        public float _x;
        public float _y;
        public float _z;
        public float _w;
        public Quaternion ToQuaterion()
        {
            return new Quaternion(_x, _y, _z, _w);
        }
        public Vector3 ToVector3()
        {
            return new Vector3(_x, _y, _z);
        }
    }

    private void Awake()
    {
        if (ReplayHand != null && replayTransform == null)
        {
            replayTransform = ReplayHand.GetComponentsInChildren<Transform>().ToDictionary(x => x.name, x => x);
            InitialVectors = StoreAngles(replayTransform);
        }
        if (skeleton == null)
        {
            skeleton = this.gameObject.GetComponent<OVRSkeleton>();
        }
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        if (_bones == null)
        {
            _bones = skeleton.Bones;
        }
        if (_dataProvider == null)
        {
            _dataProvider = GetComponent<IOVRSkeletonDataProvider>();
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private Dictionary<string, AnglesStore> StoreAngles(Dictionary<string, Transform> replayTransform)
    {
        Dictionary<string, AnglesStore> angles = new Dictionary<string, AnglesStore>();
        foreach (var a in replayTransform)
        {
            var ang = a.Value.localRotation;
            angles.Add(a.Key, new AnglesStore(ang.x, ang.y, ang.z, ang.w));
        }
        return angles;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log((string)Input.inputString);
            SetDelta = true;
        }
        UpdateBones();
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger))
        {
            Debug.Log("trigger clicked");

            AddKeyFrameTransforms(this.gameObject);
        }
    }

    void UpdateBones()
    {
        var data = _dataProvider.GetSkeletonPoseData();
        if (_bones.Count == 0 || replayTransform == null)
        {
            _bones = skeleton.Bones;
            replayTransform = ReplayHand.GetComponentsInChildren<Transform>().ToDictionary(x => x.name, x => x);
        }
        if (SetDelta == true)
        {
            OffsetDelta = new Dictionary<string, AnglesStore>();
        }
        for (var i = 0; i < _bones.Count; ++i)
        {
            if (_bones[i].Transform != null)
            {
                _bones[i].Transform.localRotation = data.BoneRotations[i].FromFlippedXQuatf();
                MapBoneToPreviewModel(_bones[i], data.BoneRotations[i], replayTransform);
                if (_bones[i].Id == BoneId.Hand_WristRoot)
                {
                    _bones[i].Transform.localRotation *= wristFixupRotation;
                }
            }
        }

        if (SetDelta)
        { SetDelta = false; }
    }

    private void MapBoneToPreviewModel(OVRBone oVRBone, Quatf quatf, Dictionary<string, Transform> replayTransforms)
    {
        Transform target = null;
        switch (oVRBone.Id)
        {
            case BoneId.Hand_Thumb1:
                {
                    target = replayTransforms["hands:b_r_thumb1"];
                    break;
                }
            case BoneId.Hand_Thumb2:
                {
                    target = replayTransforms["hands:b_r_thumb2"];
                    break;
                }
            case BoneId.Hand_Thumb3:
                {
                    target = replayTransforms["hands:b_r_thumb3"];
                    break;
                }
            case BoneId.Hand_Index1:
                {
                    target = replayTransform["hands:b_r_index1"];
                    break;
                }
            case BoneId.Hand_Index2:
                {
                    target = replayTransform["hands:b_r_index2"];
                    break;
                }
            case BoneId.Hand_Index3:
                {
                    target = replayTransform["hands:b_r_index3"];
                    break;
                }
            case BoneId.Hand_Middle1:
                {
                    target = replayTransform["hands:b_r_middle1"];
                    break;
                }
            case BoneId.Hand_Middle2:
                {
                    target = replayTransform["hands:b_r_middle2"];
                    break;
                }
            case BoneId.Hand_Middle3:
                {
                    target = replayTransform["hands:b_r_middle3"];
                    break;
                }
            case BoneId.Hand_Pinky0:
                {
                    target = replayTransform["hands:b_r_pinky0"];
                    break;
                }
            case BoneId.Hand_Pinky1:
                {
                    target = replayTransform["hands:b_r_pinky1"];
                    break;
                }
            case BoneId.Hand_Pinky2:
                {
                    target = replayTransform["hands:b_r_pinky2"];
                    break;
                }
            case BoneId.Hand_Pinky3:
                {
                    target = replayTransform["hands:b_r_pinky3"];
                    break;
                }
            case BoneId.Hand_Ring1:
                {
                    target = replayTransform["hands:b_r_ring1"];
                    break;
                }
            case BoneId.Hand_Ring2:
                {
                    target = replayTransform["hands:b_r_ring2"];
                    break;
                }
            case BoneId.Hand_Ring3:
                {
                    target = replayTransform["hands:b_r_ring3"];
                    break;
                }

        }
        if (target != null)
        {
            if (SetDelta && OffsetDelta != null)
            {
                OffsetDelta.Add(target.name, new AnglesStore(InitialVectors[target.name].ToQuaterion().eulerAngles - quatf.FromFlippedXQuatf().eulerAngles));
            }
            // the tracking model impliments by flipping X
            //_bones[i].Transform.localRotation = data.BoneRotations[i].FromFlippedXQuatf();

            // find delta between two models from base position then serialize the constant value for reuse

            // multiply newVector by the inverse of the delta vector == subtracting the offset from the final calculation
            if (OffsetDelta != null)
            {
                target.localRotation = quatf.FromFlippedXQuatf() * OffsetDelta[target.name].ToQuaterion();
            }
        }
    }

    OVRSkeleton.SkeletonPoseData OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonPoseData()
    {
        var data = new OVRSkeleton.SkeletonPoseData();

        IsDataValid = data.IsDataValid;

        if (IsDataValid)
        {
            data.RootPose = _handState.RootPose;
            data.RootScale = _handState.HandScale;
            data.BoneRotations = _handState.BoneRotations;
            bool IsTracked = false;
            TrackingConfidence HandConfidence = default;
            data.IsDataHighConfidence = IsTracked && HandConfidence == TrackingConfidence.High;
        }

        return data;
    }

    public void AddKeyFrameTransforms(GameObject Model)
    {
        var transforms = Model.GetComponentsInChildren<Transform>();
        var fingeres = transforms.Where(x => !x.name.ToLower().Contains("ignore") && (x.name.ToLower().Contains("0")
         || x.name.ToLower().Contains("1")
         || x.name.ToLower().Contains("2")
         || x.name.ToLower().Contains("3"))).ToList();
        if (TransformsHolder == null)
        {
            TransformsHolder = new ArrayList();
        }
        TransformsHolder.Add(transforms);
    }

    public void SaveAsset()
    {

    }

    public OVRSkeleton.SkeletonType GetSkeletonType()
    {
        switch (HandType)
        {
            case Hand.HandLeft:
                return OVRSkeleton.SkeletonType.HandLeft;
            case Hand.HandRight:
                return OVRSkeleton.SkeletonType.HandRight;
            case Hand.None:
            default:
                return OVRSkeleton.SkeletonType.None;
        }
    }
}
