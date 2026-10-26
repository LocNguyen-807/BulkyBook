using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BulkyBookModels
{
    public class ShoppingCart
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }

        [Range(1,1000, ErrorMessage = "Please enter number between 1 and 1000")]
        public int Count { get; set; }

        public string ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        [NotMapped]
        public double Price
        {
            get
            {
                if (this.Product == null) return 0;
                if(Count < 50)
                {
                    return this.Product.Price;
                }
                else  if (Count < 100)
                {
                    return this.Product.Price50;
                }
                else
                {
                    return this.Product.Price100;
                }
            }
        }

    }
}
