namespace XREngine.Components.Logic.Scripting
{
    public class PythonScriptComponent : ScriptComponent
    {
        public override bool Execute(string methodName, params object[] args)
        {
            return false;
        }

        //public void Execute()
        //{
        //    IronPython.Execute(Text);
        //}
        //public void Execute(string methodName, params object[] args)
        //{
        //    IronPython.Execute(Text, methodName, args);
        //}
        //public void Execute(ScriptExecInfo info)
        //{
        //    if (info != null)
        //        Execute(info.MethodName, info.Arguments);
        //}
        //public void Compile()
        //{
        //    ScriptSource source = IronPython.Engine.CreateScriptSourceFromString(Text);
        //    CompiledCode code = source.Compile(new IronPythonCompileLogger());
        //}
    }
}
