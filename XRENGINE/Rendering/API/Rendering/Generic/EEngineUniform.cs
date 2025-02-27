namespace XREngine.Rendering
{
    public enum EEngineUniform
    {
        UpdateDelta,

        //Multiply together in the shader
        ModelMatrix,

        //ViewMatrix, //Desktop
        InverseViewMatrix, //Desktop
        LeftEyeViewMatrix, //Stereo
        RightEyeViewMatrix, //Stereo

        ProjMatrix, //Desktop
        LeftEyeProjMatrix, //VR
        RightEyeProjMatrix, //VR

        //Multiply together in the shader
        PrevModelMatrix,

        PrevViewMatrix, //Desktop
        PrevLeftEyeViewMatrix, //Stereo
        PrevRightEyeViewMatrix, //Stereo
        
        PrevProjMatrix, //Desktop
        PrevLeftEyeProjMatrix, //VR
        PrevRightEyeProjMatrix, //VR

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

        RootInvModelMatrix,
        CameraForward,
        CameraUp,
    }
}