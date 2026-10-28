using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Models;

[Table("assistantes")]
public partial class Assistante
{
    [Key]
    [Column("prenom_nom")]
    [StringLength(100)]
    public string PrenomNom { get; set; } = null!;

    [Column("service")]
    [StringLength(30)]
    public string? Service { get; set; }

    [Column("all_filtres")]
    public bool AllFiltres { get; set; }

    [Column("commentaire")]
    [StringLength(50)]
    public string? Commentaire { get; set; }
}
