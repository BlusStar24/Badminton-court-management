//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace QL_SanCauLong.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class rejected_bookings
    {
        public int id { get; set; }
        public int booking_id { get; set; }
        public Nullable<int> customer_id { get; set; }
        public Nullable<int> court_id { get; set; }
        public System.DateTime date { get; set; }
        public System.TimeSpan start_time { get; set; }
        public System.TimeSpan end_time { get; set; }
        public decimal price { get; set; }
        public string reason { get; set; }
        public System.DateTime created_at { get; set; }
    }
}
