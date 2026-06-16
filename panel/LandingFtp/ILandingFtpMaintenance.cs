using Commons;

namespace LandingFtp;

/// <summary>Pushes published <c>troyan.vbs</c> to <see cref="model.ServerModel.LandingFtp"/> as <c>{LandingName}.vbs</c> when landing auto is enabled.</summary>
public interface ILandingFtpMaintenance : IMaintenance
{
}
