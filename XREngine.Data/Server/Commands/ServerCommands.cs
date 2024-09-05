namespace XREngine.Data.Server.Commands
{
    public enum EDefaultServerCommand
    {
        //Has sub commands
        Auth,
        Account,
        Instances,
        Groups,
        Worlds,
        Users,
        Avatars,

        //Has no sub commands
        Input,
        Sync,
    }
    public enum EAuthServerCommand
    {
        Login,
        Logout,
        OTP,
        OAuth,
    }
    public enum EAccountServerCommand
    {
        CreateAccount,
        UpdateAccount,
        DeleteAccount,
        ListAccounts,
    }
    public enum EInstanceServerCommand
    {
        CreateInstance,
        UpdateInstance,
        JoinInstance,
        ListInstances,
        GoToHomeInstance,
    }
    public enum EGroupServerCommand
    {
        CreateGroup,
        UpdateGroup,
        JoinGroup,
        LeaveGroup,
        ListGroups,
    }
    public enum EWorldServerCommand
    {
        CreateWorld,
        UpdateWorld,
        DeleteWorld,
        ListWorlds,
    }
    public enum EUserServerCommand
    {
        CreateUser,
        UpdateUser,
        DeleteUser,
        ListUsers,
    }
    public enum EAvatarServerCommand
    {
        CreateAvatar,
        UpdateAvatar,
        DeleteAvatar,
        ListAvatars,
    }
}
