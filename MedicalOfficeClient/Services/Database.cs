using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace MedicalOfficeClient.Services
{
    public class Database : DbContext
    {
      public DbSet<Person> Persons { get; set; }

      public DbSet<MedicalCase> MedicalCases { get; set; }

      public DbSet<MedicalItem> MedicalItems { get; set; }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
        if (!optionsBuilder.IsConfigured)
        {
          optionsBuilder.UseSqlite("Filename=sqlite.019");
        }
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
        modelBuilder.Entity<Person>().HasKey(p => new { p.PersonId });
        //modelBuilder.Entity<Person>().Property(p => p.Created).HasDefaultValueSql("datetime('now')");
        //modelBuilder.Entity<Person>().Property(p => p.Changed).HasDefaultValueSql("datetime('now')");
        //modelBuilder.Entity<Person>().Property(p => p.Changed).HasComputedColumnSql("datetime('now')");

        modelBuilder.Entity<MedicalCase>().HasKey(c => new { c.MedicalCaseId });
        //modelBuilder.Entity<MedicalCase>().Property(c => c.Created).HasDefaultValueSql("datetime('now')");
        //modelBuilder.Entity<MedicalCase>().Property(c => c.Changed).HasDefaultValueSql("datetime('now')");
        //modelBuilder.Entity<MedicalCase>().Property(c => c.Created).HasDefaultValue(DateTime.UtcNow);
        //modelBuilder.Entity<MedicalCase>().Property(c => c.Changed).HasDefaultValue(DateTime.UtcNow);
        //modelBuilder.Entity<MedicalCase>().Property(c => c.Changed).ValueGeneratedOnAddOrUpdate();
        //modelBuilder.Entity<MedicalCase>().Property(p => p.Changed).HasComputedColumnSql("datetime('now')");

        modelBuilder.Entity<MedicalItem>().HasKey(i => new { i.MedicalItemId });
        //modelBuilder.Entity<MedicalItem>().Property(i => i.Created).HasDefaultValueSql("datetime('now')");
        //modelBuilder.Entity<MedicalItem>().Property(i => i.Changed).HasDefaultValueSql("datetime('now')");
        //modelBuilder.Entity<MedicalItem>().Property(i => i.Created).HasDefaultValue(DateTime.UtcNow);
        //modelBuilder.Entity<MedicalItem>().Property(i => i.Changed).HasDefaultValue(DateTime.UtcNow);
        //modelBuilder.Entity<MedicalItem>().Property(i => i.Changed).ValueGeneratedOnAddOrUpdate();
        //modelBuilder.Entity<MedicalItem>().Property(i => i.Changed).Metadata.IsReadOnlyAfterSave = false;
        //modelBuilder.Entity<MedicalItem>().Property(i => i.Changed).HasComputedColumnSql("datetime('now')");
        //modelBuilder.Entity<MedicalItem>().Property(i => i.Changed).HasDefaultValueSql("datetime('now')");
        //modelBuilder.Entity<MedicalItem>().Property(i => i.Changed).HasDefaultValue(DateTime.UtcNow);
      }

      public override int SaveChanges()
      {
        AddTimeStamps();
        return base.SaveChanges();
      }

      public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
      {
        AddTimeStamps();
        return base.SaveChangesAsync(cancellationToken);
      }

      public override int SaveChanges(bool acceptAllChangesOnSuccess)
      {
        return base.SaveChanges(acceptAllChangesOnSuccess);
      }

      public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
      {
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
      }

      private void AddTimeStamps()
      {
        var entities = ChangeTracker.Entries()
          .Where(x => x.Entity is BaseEntity && (x.State == EntityState.Added || x.State == EntityState.Modified));

        foreach (var entity in entities)
        {
          if (entity.State == EntityState.Added)
          {
            (entity.Entity as BaseEntity).DateCreated = DateTime.UtcNow;
            (entity.Entity as BaseEntity).UserCreated = "Thomas";
          }

          (entity.Entity as BaseEntity).DateChanged = DateTime.UtcNow;
          (entity.Entity as BaseEntity).UserChanged = "Thomas";
        }
      }
    }

    public class BaseEntity
    {
      public string UserCreated { get; set; }
      public string UserChanged { get; set; }

      public DateTime? DateCreated { get; set; }
      public DateTime? DateChanged { get; set; }
    }

    public class Person : BaseEntity
    {
      [Key]
      [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
      public Guid PersonId { get; set; }

      //[Required]
      ////[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
      //public DateTime Created { get; set; }

      //[Required]
      ////[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
      //public DateTime Changed { get; set; }

      [Required]
      public String LastName { get; set; }

      [Required]
      public String FirstName { get; set; }

      [Required]
      public DateTime Birthday { get; set; }

      public String Title { get; set; }

      public List<MedicalCase> Cases { get; set; }

      public String Name
      {
        get { return FullName; }
      }

      public String FormName
      {
        get { return string.Format("{0}{1}, {2}", Title != null ? string.Format("({0}) ", Title) : "", LastName, FirstName); }
      }

      public String FullName
      {
        get { return string.Format("{0}{1} {2}", Title ?? "", FirstName, LastName); }
      }

      public override string ToString()
      {
        return this.FormName;
      }

      //public JsonObject ToJsonObject()
      //{
      //  JsonObject jsonObject = new JsonObject();

      //  jsonObject.SetNamedValue(keyId, JsonValue.CreateStringValue(this.PersonId.ToString()));
      //  jsonObject.SetNamedValue(keyLast, JsonValue.CreateStringValue(this.LastName));
      //  jsonObject.SetNamedValue(keyFirst, JsonValue.CreateStringValue(this.FirstName));
      //  jsonObject.SetNamedValue(keyBirth, JsonValue.CreateNumberValue(this.Birthday.ToBinary()));

      //  if (!String.IsNullOrEmpty(this.Title))
      //  {
      //    jsonObject.SetNamedValue(keyTitle, JsonValue.CreateStringValue(this.Title));
      //  }

      //  if (Cases != null && 0 < Cases.Count)
      //  {
      //    var cases = new JsonArray();
      //    foreach (var item in this.Cases)
      //    {
      //      cases.Add(item.ToJsonObject());
      //    }
      //    jsonObject[keyCases] = cases;
      //  }

      //  return jsonObject;
      //}

      //public static Person FromJsonObject(string jsonString)
      //{
      //  var jsonObject = JsonObject.Parse(jsonString);

      //  var PersonId = Convert.ToInt32(jsonObject.GetNamedString(keyId));
      //  var LastName = jsonObject.GetNamedString(keyLast);
      //  var FirstName = jsonObject.GetNamedString(keyFirst);
      //  var Birthday = DateTime.FromBinary(Convert.ToInt64(jsonObject.GetNamedNumber(keyBirth)));
      //  var Title = jsonObject.ContainsKey(keyTitle) ? jsonObject.GetNamedString(keyTitle) : null;

      //  return new Person
      //  {
      //    PersonId = Guid.Parse(jsonObject.GetNamedString(keyId)),
      //    LastName = jsonObject.GetNamedString(keyLast),
      //    FirstName = jsonObject.GetNamedString(keyFirst),
      //    Birthday = DateTime.FromBinary(Convert.ToInt64(jsonObject.GetNamedNumber(keyBirth))),
      //    Title = jsonObject.ContainsKey(keyTitle) ? jsonObject.GetNamedString(keyTitle) : null
      //  };
      //}

      //private const string keyId = "ID";
      //private const string keyCreated = "Created";
      //private const string keyChanged = "Changed";
      //private const string keyLast = "Last";
      //private const string keyFirst = "First";
      //private const string keyBirth = "Birth";
      //private const string keyTitle = "Title";
      //private const string keyCases = "Cases";
    }

    public class MedicalCase : BaseEntity
    {
      [Key]
      [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
      public Guid MedicalCaseId { get; set; }

      [Required]
      public MedicalCaseType Type { get; set; }

      //[Required]
      //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
      public string Label { get; set; }

      //[Required]
      ////[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
      //public DateTime Created { get; set; }

      //[Required]
      //public DateTime? Changed { get; set; }

      public Guid PersonId { get; set; }

      public Person Person { get; set; }

      public List<MedicalItem> Items { get; set; }

      //public JsonObject ToJsonObject()
      //{
      //  JsonObject jsonObject = new JsonObject();

      //  jsonObject.SetNamedValue(keyId, JsonValue.CreateStringValue(MedicalCaseId.ToString()));
      //  jsonObject.SetNamedValue(keyCreated, JsonValue.CreateNumberValue(Created.ToBinary()));
      //  //jsonObject.SetNamedValue(keyChanged, JsonValue.CreateNumberValue(Changed.ToBinary()));
      //  jsonObject.SetNamedValue(keyLabel, JsonValue.CreateStringValue(Label));

      //  if (Items != null && 0 < Items.Count)
      //  {
      //    var list = new JsonArray();
      //    foreach (var item in Items)
      //    {
      //      list.Add(item.ToJsonObject());
      //    }
      //    jsonObject[keyItems] = list;
      //  }

      //  return jsonObject;
      //}

      //private const string keyId = "ID";
      //private const string keyLabel = "Label";
      //private const string keyCreated = "Created";
      //private const string keyChanged = "Changed";
      //private const string keyItems = "Items";

      public SolidColorBrush ColorBrush { get { return GetColor(this.Type); } }

      public SolidColorBrush GetColor(MedicalCaseType type)
      {
        switch (type)
        {
          case MedicalCaseType.Logo: return new SolidColorBrush(Color.FromArgb(255, 148, 5, 135));
          case MedicalCaseType.Ergo: return new SolidColorBrush(Color.FromArgb(255, 81, 122, 167));
          case MedicalCaseType.Physio: return new SolidColorBrush(Color.FromArgb(255, 169, 201, 55));
          case MedicalCaseType.Satellite: return new SolidColorBrush(Color.FromArgb(255, 250, 163, 35));
          default: return new SolidColorBrush(Colors.Gray);
        }
      }

      public static SolidColorBrush TypeColor(MedicalCaseType type)
      {
        return new MedicalCase().GetColor(type);
      }
    }

    public enum MedicalCaseType
    {
      Logo, // logotherapy, speech
      Ergo, // ergotherapy
      Physio,  // physiotherapy
      Satellite // satellite
    }

    public class MedicalItem : BaseEntity
    {
      [Key]
      [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
      public Guid MedicalItemId { get; set; }

      //[Required]
      public MedicalItemType Type { get; set; }

      //[Required]
      //public DateTime Created { get; set; }

      //[Required]
      //public DateTime Changed { get; set; }

      public string Label { get; set; }

      public byte[] Content { get; set; }

      public byte[] Overlay { get; set; }

      public byte[] Preview { get; set; }

      public byte[] Element { get; set; }

      public Guid MedicalCaseId { get; set; }

      public MedicalCase MedicalCase { get; set; }

      //public JsonObject ToJsonObject()
      //{
      //  var jsonObject = new JsonObject();

      //  jsonObject.SetNamedValue(keyId, JsonValue.CreateStringValue(MedicalItemId.ToString()));
      //  jsonObject.SetNamedValue(keyCreated, JsonValue.CreateNumberValue(Created.ToBinary()));
      //  jsonObject.SetNamedValue(keyChanged, JsonValue.CreateNumberValue(Changed.ToBinary()));
      //  jsonObject.SetNamedValue(keyType, JsonValue.CreateStringValue(Type.ToString()));

      //  jsonObject.SetNamedValue(keyContent, JsonValue.CreateStringValue(Convert.ToBase64String(Content ?? new byte[0])));
      //  jsonObject.SetNamedValue(keyPreview, JsonValue.CreateStringValue(Convert.ToBase64String(Preview ?? new byte[0])));
      //  jsonObject.SetNamedValue(keyOverlay, JsonValue.CreateStringValue(Convert.ToBase64String(Overlay ?? new byte[0])));

      //  return jsonObject;
      //}

      //private const string keyId = "ID";
      //private const string keyType = "Type";
      //private const string keyCreated = "Created";
      //private const string keyChanged = "Changed";
      //private const string keyContent = "Content";
      //private const string keyPreview = "Preview";
      //private const string keyOverlay = "Overlay";

      public SolidColorBrush ColorBrush { get { return GetColor(this.Type); } }

      public SolidColorBrush GetColor(MedicalItemType type)
      {
        switch (type)
        {
          case MedicalItemType.Label: return new SolidColorBrush(Colors.MediumBlue);
          case MedicalItemType.Image: return new SolidColorBrush(Colors.MediumOrchid);
          case MedicalItemType.Document: return new SolidColorBrush(Colors.RosyBrown);
          default: return new SolidColorBrush(Colors.Gray);
        }
      }

      public static SolidColorBrush TypeColor(MedicalItemType type)
      {
        return new MedicalItem().GetColor(type);
      }

      public Symbol Symbol { get { return GetSymbol(this.Type); } }

      public Symbol GetSymbol(MedicalItemType type)
      {
        switch (type)
        {
          case MedicalItemType.Label: return Symbol.Page;
          case MedicalItemType.Image: return Symbol.Pictures;
          case MedicalItemType.Document: return Symbol.Document;
          default: return Symbol.Help;
        }
      }

      public static Symbol TypeSymbol(MedicalItemType type)
      {
        return new MedicalItem().GetSymbol(type);
      }
    }

    public enum MedicalItemType
    {
      //None,
      Label,  // reiner text
      Image,  // Grafikabbild + Overlay
              //Paper,  // Leeres Blatt + Overlay
              //Photo,  // Photographie + Overlay
              //Import, // Importbilder + Overlay
      Document
    }

}
