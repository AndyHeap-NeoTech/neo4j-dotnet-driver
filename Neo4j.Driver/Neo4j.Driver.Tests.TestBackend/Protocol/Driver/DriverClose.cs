using System;
using System.Threading.Tasks;

using Neo4j_TestBackendDriverInterface;


namespace Neo4j.Driver.Tests.TestBackend
{ 
    internal class DriverClose : IProtocolObject
    {
        public DriverCloseType data { get; set; } = new DriverCloseType();
        
        public class DriverCloseType
        {
            public string driverId { get; set; }            
        }

        public override async Task Process()
        {
            DriverInterface driver = ((NewDriver)ObjManager.GetObject(data.driverId)).Driver;
            await driver.CloseAsync();                            
        }

        public override string Respond()
        {   
            return new ProtocolResponse("Driver", uniqueId).Encode();
        }
    }
}
