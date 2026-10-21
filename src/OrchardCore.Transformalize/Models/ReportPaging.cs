using TransformalizeModule.Ext;

namespace TransformalizeModule.Models;

// User paging preferences must not be inferred from an entity's internal fetch size.
public sealed class ReportPaging {
   public ReportPaging(IEnumerable<int> sizes, bool fetchPaged = true) {
      Sizes = sizes.Where(size => size > 0).Distinct().Order().ToArray();
      FetchPaged = fetchPaged && Sizes.Length > 0;
   }

   public int[] Sizes { get; }
   public bool Enabled => Sizes.Length > 0;
   public bool FetchPaged { get; }
   public int DefaultSize => Sizes.FirstOrDefault();

   public int NormalizeSize(string? requested) {
      if (!Enabled || !int.TryParse(requested, out var size)) return DefaultSize;
      if (Sizes.Contains(size)) return size;
      // Retain the existing upper bound protection, and default other invalid sizes.
      return size > Sizes[^1] ? Sizes[^1] : DefaultSize;
   }

   public string SizeParameter(string? requested) {
      var size = NormalizeSize(requested);
      return size == DefaultSize ? string.Empty : size.ToString(System.Globalization.CultureInfo.InvariantCulture);
   }

   public static IEnumerable<int> ReportSizes(TransformalizeReportPart part, TransformalizeSettings settings) =>
      !part.PageSizes.Enabled() ? [] : part.PageSizes.OverrideDefaults()
         ? part.PageSizes.SplitIntegers(',') : settings.DefaultPageSizesAsEnumerable();

   public static IEnumerable<int> ExtendedSizes(TransformalizeReportPart part, TransformalizeSettings settings) =>
      !part.PageSizesExtended.Enabled() ? [] : part.PageSizesExtended.OverrideDefaults()
         ? part.PageSizesExtended.SplitIntegers(',') : settings.DefaultPageSizesExtendedAsEnumerable();

   public static ReportPaging For(TransformalizeReportPart part, TransformalizeSettings settings, string mode) {
      // Charts use the table paging preference for navigation, although they fetch one row.
      if (mode is not ("map" or "calendar")) return new(ReportSizes(part, settings), mode != "chart");
      return part.PageSizesExtended.Enabled()
         ? new(ReportSizes(part, settings).Concat(ExtendedSizes(part, settings)))
         : new(ReportSizes(part, settings), fetchPaged: false);
   }
}
