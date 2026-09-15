using Lumina.Core;
using Xunit;

namespace Lumina.Core.Tests;

public sealed class ManagedContentTests
{
    [Fact]
    public void Local_Record_Has_No_Project_Id()
    {
        var record = ManagedContentRecord.Local("example", ContentKind.Mod);
        Assert.True(record.IsLocalOnly);
        Assert.Null(record.ProjectId);
    }
}
