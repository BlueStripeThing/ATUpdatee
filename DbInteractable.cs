using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATUpdaterBot
{
    internal class DbInteractable // Многовероятно превратить в интерфейс
    {
        public virtual bool ToDB(String connectionString) { return false; }
        public virtual bool CheckDB(String connectionString) { return false; }
    }
}
