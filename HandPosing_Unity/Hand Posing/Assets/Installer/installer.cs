using UnityEngine;
using Zenject;
using static AnimationVectorAnalyser;
using PoseAuthoring.HandAnimation.Interfaces;
using PoseAuthoring.HandAnimation.CaptureServices;
using PoseAuthoring.HandAnimation.AnimationGenerators;

public class installer : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<IVectorGraphService>()
            .To<LineEngine>()
            .AsSingle();
        Container.Bind<IHandAnimationCaptureService>()
            .To<SmoothOverTimeHandCapureService>()
            .AsSingle();
        //Container.Bind<IHandAnimationGenerater>().To<GenerateFromPoseWithTime>().AsSingle();
    }
}