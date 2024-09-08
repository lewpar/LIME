using LIME.Shared.Models;
using LIME.Shared.Network;

using System.Text;

namespace LIME.Mediator.Network.Packets;

public class TaskPacket : ILimePacket
{
    private readonly LimeTask task;

    public TaskPacket(LimeTask task)
    {
        this.task = task;
    }

    public byte[] Serialize()
    {
        var ms = new MemoryStream();

        ms.Write(BitConverter.GetBytes((int)LimeOpCodes.SMSG_TASK));
        ms.Write(BitConverter.GetBytes((int)task.Type));

        if(task.Args is not null)
        {
            var args = Encoding.UTF8.GetBytes(task.Args);
            ms.Write(BitConverter.GetBytes(args.Length));
            ms.Write(args);
        }

        return ms.ToArray();
    }
}
