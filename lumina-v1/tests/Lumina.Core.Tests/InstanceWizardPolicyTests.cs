using Lumina.Core;
using Xunit;

namespace Lumina.Core.Tests;

public sealed class InstanceWizardPolicyTests
{
    [Fact]
    public void Signed_Out_User_Cannot_Create_Instance()
    {
        Assert.False(InstanceWizardPolicy.CanCreateInstance(
            signedIn: false,
            name: "Fabric",
            validation: InstanceValidationResult.Ok()));
    }

    [Fact]
    public void Blank_Name_Cannot_Create_Instance()
    {
        Assert.False(InstanceWizardPolicy.CanCreateInstance(
            signedIn: true,
            name: "   ",
            validation: InstanceValidationResult.Ok()));
    }

    [Fact]
    public void Invalid_Profile_Cannot_Create_Instance()
    {
        Assert.False(InstanceWizardPolicy.CanCreateInstance(
            signedIn: true,
            name: "Fabric",
            validation: InstanceValidationResult.Fail("invalid")));
    }

    [Fact]
    public void Signed_In_Valid_Profile_With_Name_Can_Be_Created()
    {
        Assert.True(InstanceWizardPolicy.CanCreateInstance(
            signedIn: true,
            name: "Fabric",
            validation: InstanceValidationResult.Ok()));
    }
}
