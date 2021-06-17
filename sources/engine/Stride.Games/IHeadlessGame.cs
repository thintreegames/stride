using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Games
{
    public interface IHeadlessGame : IGame
    {
        void Run(string scene);
        void Tick();
        void Exit();
    }
}
