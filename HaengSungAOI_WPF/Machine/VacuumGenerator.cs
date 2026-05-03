using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaengSungAOI_WPF.Machine
{
    public class VacuumGenerator
    {
        private readonly IOPin _vacuumValve;
        private readonly IOPin _vacuumSensor;

        public bool IsProductDropped => _vacuumValve.GetValue() == 0 && _vacuumSensor.GetValue() == 1;

        public VacuumGenerator(IOPin vacuumValve, IOPin vacuumSensor)
        {
            _vacuumValve = vacuumValve;
            _vacuumSensor = vacuumSensor;
        }

        public void TurnOnVacuum(int timeout)
        {
            _vacuumValve.SetValue(0);
            var startTime = DateTime.Now;
            while (!IsVacuumOn())
            {
                Task.Delay(10).Wait();
                if ((DateTime.Now - startTime).TotalMilliseconds > timeout)
                {
                    throw new TimeoutException("Vacuum sensor did not activate within the timeout period.");
                }
            }
        }

        public void TurnOffVacuum(int timeout)
        {
            _vacuumValve.SetValue(1);
            var startTime = DateTime.Now;
            while (IsVacuumOn())
            {
                Task.Delay(10).Wait();
                if ((DateTime.Now - startTime).TotalMilliseconds > timeout)
                {
                    throw new TimeoutException("Vacuum sensor did not deactivate within the timeout period.");
                }
            }
        }

        private bool IsVacuumOn()
        {
            return _vacuumSensor.GetValue() == 0;
        }
        
    }
}
