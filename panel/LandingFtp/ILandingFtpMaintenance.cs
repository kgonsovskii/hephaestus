using Commons;

namespace LandingFtp;

/// <summary>Pushes published <c>troyan.vbs</c> and <c>troyan.cmd</c> to <see cref="model.ServerModel.LandingFtp"/> as <c>{LandingName}.vbs</c> and <c>{LandingName}.cmd</c> when landing auto is enabled.</summary>
public interface ILandingFtpMaintenance : IMaintenance
{
}
