using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TestProject
{
    public partial class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public virtual ICollection<Message> ToMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> FromMessages { get; set; } = new List<Message>();
    }
}
