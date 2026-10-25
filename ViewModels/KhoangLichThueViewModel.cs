namespace ThueVayDuTiec.Web.ViewModels;

public sealed class KhoangLichThueViewModel
{
    public DateTime NgayBatDau { get; init; }
    public DateTime NgayKetThuc { get; init; }
    public DateTime NgayXuLy { get; init; }
    public DateTime NgayCoTheThueLai => NgayXuLy.AddDays(1);
}
