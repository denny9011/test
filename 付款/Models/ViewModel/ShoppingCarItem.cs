using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace project.Models.ViewModel
{
    public class ShoppingCarItem
    {
        public string UserId { get; set; }
        public int Fid { get; set; }   
        public string Pid { get; set; }
        public string Name { get; set; }
        public int Qty { get; set; }
        public int Price { get; set; }
        public string IsApproved { get; set; }  
    }
}