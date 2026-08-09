using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Services.Order.Domain.Enums
{
    public enum OrderStatus
    {
        Beklemede = 1,  
        Tamamlandı = 2,
        IptalEdildi = 3   
    }
}
