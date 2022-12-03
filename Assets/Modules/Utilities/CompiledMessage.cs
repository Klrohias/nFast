using System;

public class MESSAGE_FOR_CRACKER
{
    public const string VALUE = "嘛，方便开发和调试，所以用的是mono运行时而不是il2cpp。当然发布的时候也可能还会继续用mono！\n就当我变相开源了吧（（（\n瞎改的话标记个原作者就行，注：因为瞎改这玩意引起的一切后果原作者均不承担！";
    static MESSAGE_FOR_CRACKER()
    {
        Console.WriteLine(VALUE);
    }
}