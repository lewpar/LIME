using LIME.Shared.Models;
using LIME.Shared.Network;

namespace LIME.Mediator.Network.Packets;

public class JobPacket : ILimePacket
{
    private readonly JobType job;

    public JobPacket(JobType job)
    {
        this.job = job;
    }

    public byte[] Serialize()
    {
        var ms = new MemoryStream();

        ms.Write(BitConverter.GetBytes((int)LimeOpCodes.SMSG_JOB));
        ms.Write(BitConverter.GetBytes((int)job));

        return ms.ToArray();
    }
}
