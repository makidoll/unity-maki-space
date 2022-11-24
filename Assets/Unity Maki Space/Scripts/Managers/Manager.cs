using System.Threading.Tasks;

namespace Unity_Maki_Space.Scripts.Managers
{
    public abstract class Manager
    {
        public virtual Task Init()
        {
            return Task.CompletedTask;
        }

        public virtual void Update()
        {
            
        }

        public virtual void OnDestroy()
        {
            
        }
    }
}