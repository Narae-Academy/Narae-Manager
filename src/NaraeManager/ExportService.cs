using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NaraeManager;

public static class ExportService
{
    private static readonly Dictionary<int, HashSet<DateTime>> Holidays = new()
    {
        [2025] = new(new[]{"2025-01-01","2025-01-27","2025-01-28","2025-01-29","2025-01-30","2025-03-03","2025-05-05","2025-05-06","2025-06-06","2025-08-15","2025-10-03","2025-10-06","2025-10-07","2025-10-08","2025-10-09","2025-12-25"}.Select(DateTime.Parse)),
        [2026] = new(new[]{"2026-01-01","2026-02-16","2026-02-17","2026-02-18","2026-03-02","2026-05-05","2026-05-25","2026-06-03","2026-08-17","2026-09-24","2026-09-25","2026-10-05","2026-10-09","2026-12-25"}.Select(DateTime.Parse))
    };

    public static List<DateTime> TrainingDates(Course c)
    {
        var allowed = c.Weekdays.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
        var list = new List<DateTime>();
        for (var d = c.StartDate.Date; d <= c.EndDate.Date; d = d.AddDays(1))
            if (allowed.Contains(((int)d.DayOfWeek + 6) % 7) && (!Holidays.TryGetValue(d.Year, out var h) || !h.Contains(d))) list.Add(d);
        return list;
    }

    public static List<ScheduleRow> BuildSchedule(Course c, IReadOnlyList<CourseSubjectCode> subjects)
    {
        var dates = TrainingDates(c);
        var slots = HourSlots(c.StartTime, c.EndTime);
        var required = dates.Count * slots.Count;
        var allocated = subjects.Sum(x => x.AllocatedHours);
        if (subjects.Count == 0) throw new InvalidOperationException("교과목 코드를 등록하십시오.");
        if (allocated != required) throw new InvalidOperationException($"교과목 배정시간 합계({allocated})와 실제 총시간({required})이 다릅니다.");
        var expanded = subjects.SelectMany(s => Enumerable.Repeat(s, s.AllocatedHours)).ToList();
        var result = new List<ScheduleRow>(); var i = 0;
        foreach (var d in dates) foreach (var slot in slots)
        {
            var s = expanded[i++];
            result.Add(new(d.ToString("yyyyMMdd"), c.StartTime, c.EndTime, c.RemoteType, slot, c.TimeType, s.TeacherCode, s.RoomCode, s.SubjectCode));
        }
        return result;
    }

    public static void ExportScheduleExcel(string path, Course c, IReadOnlyList<CourseSubjectCode> subjects)
    {
        var rows = BuildSchedule(c, subjects);
        using var wb = new XLWorkbook(); var ws = wb.AddWorksheet("sheet1");
        ws.Range("A1:D1").Merge().Value = "일자별 시간표 내역을 입력합니다";
        ws.Range("E1:I1").Merge().Value = "교시별 세부 시간표 내역을 입력합니다";
        string[] headers={"훈련일자","훈련시작시간","훈련종료시간","방학/원격여부","시작시간","시간구분","훈련강사코드","교육장소(강의실)코드","교과목(및 능력단위)코드"};
        for(int i=0;i<headers.Length;i++) ws.Cell(2,i+1).Value=headers[i];
        for(int r=0;r<rows.Count;r++)
        {
            var x=rows[r]; string[] values={x.TrainingDate,x.TrainingStart,x.TrainingEnd,x.RemoteType,x.SlotStart,x.TimeType,x.TeacherCode,x.RoomCode,x.SubjectCode};
            for(int col=0;col<values.Length;col++){ws.Cell(r+3,col+1).Value=values[col];ws.Cell(r+3,col+1).Style.NumberFormat.Format="@";}
        }
        ws.RangeUsed().Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
        ws.Rows(1,2).Style.Font.Bold=true; ws.Rows(1,2).Style.Fill.BackgroundColor=XLColor.FromHtml("#DCE6F1"); ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(2); wb.SaveAs(path);
    }

    public static void ExportDiaryExcel(string path, Course c, string teacher)
    {
        var dates=TrainingDates(c); var contents=Contents(c.Category, dates.Count);
        using var wb=new XLWorkbook();var ws=wb.AddWorksheet("훈련일지");
        ws.Range("A1:G1").Merge().Value="훈 련 일 지";ws.Cell("A1").Style.Font.Bold=true;ws.Cell("A1").Style.Font.FontSize=18;ws.Cell("A1").Style.Alignment.Horizontal=XLAlignmentHorizontalValues.Center;
        ws.Cell("A2").Value="과정명";ws.Range("B2:D2").Merge().Value=c.Name;ws.Cell("E2").Value="훈련교사";ws.Range("F2:G2").Merge().Value=teacher;
        ws.Cell("A3").Value="훈련기간";ws.Range("B3:D3").Merge().Value=$"{c.StartDate:yyyy-MM-dd} ~ {c.EndDate:yyyy-MM-dd}";ws.Cell("E3").Value="회차";ws.Range("F3:G3").Merge().Value=$"{c.Round}회차";
        ws.Cell("A4").Value="과정ID";ws.Range("B4:D4").Merge().Value=c.CourseCode;ws.Cell("E4").Value="훈련일수";ws.Range("F4:G4").Merge().Value=$"{dates.Count}일";
        string[] h={"일자","요일","훈련내용","훈련방법","훈련교사","서명","비고"};for(int i=0;i<h.Length;i++)ws.Cell(6,i+1).Value=h[i];
        string[] day={"일","월","화","수","목","금","토"};
        for(int i=0;i<dates.Count;i++){var r=i+7;ws.Cell(r,1).Value=dates[i];ws.Cell(r,1).Style.DateFormat.Format="yyyy-mm-dd";ws.Cell(r,2).Value=day[(int)dates[i].DayOfWeek];ws.Cell(r,3).Value=contents[i];ws.Cell(r,4).Value="강의·실습";ws.Cell(r,5).Value=teacher;ws.Cell(r,6).Value=teacher;}
        var used=ws.RangeUsed();used.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);used.Style.Alignment.Vertical=XLAlignmentVerticalValues.Center;used.Style.Alignment.WrapText=true;
        ws.Range("A6:G6").Style.Fill.BackgroundColor=XLColor.FromHtml("#1F4E78");ws.Range("A6:G6").Style.Font.FontColor=XLColor.White;ws.Range("A6:G6").Style.Font.Bold=true;
        ws.Column(1).Width=14;ws.Column(2).Width=7;ws.Column(3).Width=44;ws.Columns(4,7).Width=13;ws.PageSetup.PageOrientation=XLPageOrientation.Portrait;ws.PageSetup.PaperSize=XLPaperSize.A4Paper;ws.PageSetup.FitToPages(1,0);wb.SaveAs(path);
    }

    public static void ExportDiaryPdf(string path, Course c, string teacher)
    {
        var dates=TrainingDates(c);var contents=Contents(c.Category,dates.Count);string[] day={"일","월","화","수","목","금","토"};
        Document.Create(doc=>doc.Page(page=>{page.Size(PageSizes.A4);page.Margin(25);page.DefaultTextStyle(x=>x.FontSize(9));page.Header().AlignCenter().Text("훈 련 일 지").Bold().FontSize(18);page.Content().Column(col=>{
            col.Item().PaddingVertical(8).Text($"과정명: {c.Name}\n훈련기간: {c.StartDate:yyyy-MM-dd} ~ {c.EndDate:yyyy-MM-dd}    회차: {c.Round}회차    훈련교사: {teacher}");
            col.Item().Table(t=>{t.ColumnsDefinition(x=>{x.ConstantColumn(65);x.ConstantColumn(35);x.RelativeColumn(4);x.ConstantColumn(65);x.ConstantColumn(55);x.ConstantColumn(55);x.ConstantColumn(45);});
                foreach(var h in new[]{"일자","요일","훈련내용","훈련방법","훈련교사","서명","비고"})t.Cell().Background("#1F4E78").Padding(4).Text(h).FontColor(Colors.White).Bold();
                for(int i=0;i<dates.Count;i++){foreach(var v in new[]{dates[i].ToString("yyyy-MM-dd"),day[(int)dates[i].DayOfWeek],contents[i],"강의·실습",teacher,teacher,""})t.Cell().Border(0.5f).Padding(3).Text(v);}
            });
        });page.Footer().AlignCenter().Text(x=>{x.Span("페이지 ");x.CurrentPageNumber();});})).GeneratePdf(path);
    }

    private static List<string> HourSlots(string start,string end){int ToMin(string x)=>int.Parse(x[..2])*60+int.Parse(x[2..]);string ToText(int x)=>$"{x/60:00}{x%60:00}";var list=new List<string>();for(var m=ToMin(start);m<ToMin(end);m+=60)list.Add(ToText(m));return list;}
    private static List<string> Contents(string category,int n){var baseList=category switch{"ITQ"=>new[]{"파워포인트 기본 기능 실습","슬라이드·도형·차트 작성 실습","파워포인트 기출문제 실습","한글 문서작성 및 서식 실습","한글 표·도형 편집 실습","한글 기출문제 실습","엑셀 데이터 입력 및 셀 서식","엑셀 함수와 데이터 관리 실습","엑셀 차트 및 기출문제 실습","ITQ 종합 모의시험"},"CAD"=>new[]{"AutoCAD 작업환경 이해","기본 명령어 실습","객체 편집 명령어 실습","레이어 및 치수기입 실습","2D 도면 작성 실습","3D 모델링 실습","CAT 2급 모의문제"},"컴활"=>new[]{"엑셀 기본 작업 실습","함수 활용 실습","분석 작업 실습","차트 및 매크로 실습","기출문제 실습"},"영상"=>new[]{"프리미어프로 기본 편집","컷 편집과 자막 실습","영상 효과와 색상 보정","오디오 및 출력 실습","홍보영상 종합 제작"},_=>new[]{"과정 오리엔테이션 및 기본 기능","콘텐츠 기획 실습","도구 활용 실습","결과물 제작 실습","종합 실습 및 점검"}};var result=new List<string>();for(int i=0;i<n;i++)result.Add(baseList[Math.Min(baseList.Length-1,(int)Math.Floor(i*(double)baseList.Length/n))]);return result;}
}
