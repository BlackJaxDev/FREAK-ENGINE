namespace XREngine.Data.Rendering
{
    public enum EEngineUniform
    {
        UpdateDelta,

        ModelMatrix,
        WorldToCameraSpaceMatrix,
        ProjMatrix,
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
