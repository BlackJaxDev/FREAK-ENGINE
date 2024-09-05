namespace XREngine.Rendering
{
    public enum EEngineUniform
    {
        UpdateDelta,

        ModelMatrix,
        WorldToCameraSpaceMatrix,
        ProjMatrix, //Desktop
        LeftEyeProjMatrix, //VR
        RightEyeProjMatrix, //VR
        NormalMatrix,

        InvModelMatrix,
        CameraToWorldSpaceMatrix,
        InvProjMatrix,

        PrevModelMatrix,
        PrevViewMatrix,
        PrevProjMatrix,

        PrevInvModelMatrix,
        PrevInvViewMatrix,
        PrevInvProjMatrix,

        ScreenWidth,
        ScreenHeight,
        ScreenOrigin,

        CameraFovX,
        CameraFovY,
        CameraAspect,
        CameraNearZ,
        CameraFarZ,
        CameraPosition,
        //CameraForward,
        //CameraUp,
        //CameraRight,
    }
}