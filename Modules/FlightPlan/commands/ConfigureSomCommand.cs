namespace SatOps.Modules.FlightPlan.Commands
{
    public class ConfigureSomCommand : Command
    {
        public override string CommandType => CommandTypeConstants.ConfigureSom;
        private const int AppSysNode = 5421;

        public override Task<List<string>> CompileToCsh()
        {
            return Task.FromResult(new List<string>
            {
                $"set mng_dipp 5423 -n {AppSysNode}",
                $"set mng_camera_control 5422 -n {AppSysNode}"
            });
        }
    }
}