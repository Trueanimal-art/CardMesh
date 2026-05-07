using UnityEngine;

[CreateAssetMenu(fileName = "MetaScore", menuName = "WorkForUncle/Meta Score")]
public class MetaScore : ScriptableObject
{
    public int tihiyGeroy;
    public int corporatePolitik;
    public int prosvetlenniy;
    public int officeTrup;

    public int corporateGhost;
    public int officePhilosopher;

    // ✅ секретные
    public int callOverlord;
    public int airSalesman;
    public int vacationNinja;
    public int urgentKing;

    public void ResetScore()
    {
        tihiyGeroy = 0;
        corporatePolitik = 0;
        prosvetlenniy = 0;
        officeTrup = 0;

        corporateGhost = 0;
        officePhilosopher = 0;

        callOverlord = 0;
        airSalesman = 0;
        vacationNinja = 0;
        urgentKing = 0;
    }

    public void Add(MetaWeightsData.AnswerWeights w)
    {
        if (w == null) return;

        tihiyGeroy += w.tihiyGeroy;
        corporatePolitik += w.corporatePolitik;
        prosvetlenniy += w.prosvetlenniy;
        officeTrup += w.officeTrup;

        corporateGhost += w.corporateGhost;
        officePhilosopher += w.officePhilosopher;

        callOverlord += w.callOverlord;
        airSalesman += w.airSalesman;
        vacationNinja += w.vacationNinja;
        urgentKing += w.urgentKing;
    }

    public OfficeArchetype GetWinner()
    {
        int max = tihiyGeroy;
        OfficeArchetype win = OfficeArchetype.TihiyGeroy;

        if (corporatePolitik > max) { max = corporatePolitik; win = OfficeArchetype.CorporatePolitik; }
        if (prosvetlenniy > max) { max = prosvetlenniy; win = OfficeArchetype.Prosvetlenniy; }
        if (officeTrup > max) { max = officeTrup; win = OfficeArchetype.OfficeTrup; }

        if (corporateGhost > max) { max = corporateGhost; win = OfficeArchetype.CorporateGhost; }
        if (officePhilosopher > max) { max = officePhilosopher; win = OfficeArchetype.OfficePhilosopher; }

        // ✅ секретные
        if (callOverlord > max) { max = callOverlord; win = OfficeArchetype.CallOverlord; }
        if (airSalesman > max) { max = airSalesman; win = OfficeArchetype.AirSalesman; }
        if (vacationNinja > max) { max = vacationNinja; win = OfficeArchetype.VacationNinja; }
        if (urgentKing > max) { max = urgentKing; win = OfficeArchetype.UrgentKing; }

        return win;
    }
}
