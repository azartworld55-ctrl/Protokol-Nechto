using RimWorld;
using UnityEngine;
using Verse;

namespace AzartWorld.PrisonColony
{
    /// <summary>
    /// Lightweight vanilla-style management tab. The first build intentionally
    /// uses selected pawns as a temporary intake adapter until shuttle APIs are verified.
    /// </summary>
    public sealed class MainTabWindow_PrisonColony : MainTabWindow
    {
        private const int PrototypeCapacity = 8;

        public override Vector2 InitialSize
        {
            get { return new Vector2(620f, 520f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            PrisonColonyGameComponent state = Current.Game.GetComponent<PrisonColonyGameComponent>();
            if (state == null)
            {
                Widgets.Label(inRect, "Prison Colony: компонент сохранения недоступен.");
                return;
            }

            float y = 0f;
            Widgets.Label(new Rect(0f, y, inRect.width, 32f), "PRISON COLONY — УПРАВЛЕНИЕ");
            y += 38f;
            Widgets.Label(new Rect(0f, y, inRect.width, 26f),
                "Резерв учреждения: " + state.Credits + " кредитов    Заключённых: " +
                state.Prisoners.Count + " / " + PrototypeCapacity);
            y += 34f;

            if (Widgets.ButtonText(new Rect(0f, y, 220f, 32f), "Получить предложение"))
            {
                state.CreateOffer(2, 7, 15);
            }
            y += 42f;

            Widgets.Label(new Rect(0f, y, inRect.width, 24f), "КОНТРАКТЫ");
            y += 26f;

            foreach (PrisonContract contract in state.Contracts)
            {
                if (contract == null) continue;
                string line = "#" + contract.Id + " — " + StatusLabel(contract.CurrentStatus) +
                    " | партия: " + contract.PrisonerCount +
                    " | срок: " + contract.DurationDays + " дн." +
                    " | ставка: " + contract.DailyRate + "/заключённый/день";
                Widgets.Label(new Rect(0f, y, inRect.width, 24f), line);
                y += 26f;

                if (contract.CurrentStatus == PrisonContractStatus.Offered)
                {
                    if (Widgets.ButtonText(new Rect(0f, y, 180f, 30f), "Принять контракт"))
                    {
                        state.AcceptOffer(contract.Id, PrototypeCapacity - state.GetCommittedCapacity());
                    }
                    y += 36f;
                }
                else if (contract.CurrentStatus == PrisonContractStatus.Accepted)
                {
                    Widgets.Label(new Rect(0f, y, inRect.width, 22f),
                        "Для тестового приёма выдели пешек на карте и нажми кнопку ниже.");
                    y += 24f;
                    if (Widgets.ButtonText(new Rect(0f, y, 240f, 30f), "Принять выбранных пешек"))
                    {
                        RegisterSelectedPawns(state, contract);
                    }
                    y += 36f;
                }
                else if (contract.CurrentStatus == PrisonContractStatus.Departing)
                {
                    Widgets.Label(new Rect(0f, y, inRect.width, 22f),
                        "Срок завершён. Ожидается подключение транспортного адаптера.");
                    y += 24f;
                }
            }

            y += 8f;
            Widgets.Label(new Rect(0f, y, inRect.width, 24f),
                "ПРИМЕЧАНИЕ: ручной приём пешек — временный тестовый путь, не посадка шаттла.");
        }

        private static void RegisterSelectedPawns(PrisonColonyGameComponent state, PrisonContract contract)
        {
            foreach (Pawn pawn in Find.Selector.SelectedPawns)
            {
                if (pawn == null || pawn.guest == null || !pawn.RaceProps.Humanlike) continue;
                if (pawn.Faction == Faction.OfPlayer) continue;
                if (state.RegisterArrival(contract.Id, pawn))
                    pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
            }
        }

        private static string StatusLabel(PrisonContractStatus status)
        {
            switch (status)
            {
                case PrisonContractStatus.Offered: return "Предложен";
                case PrisonContractStatus.Accepted: return "Ожидает приёма";
                case PrisonContractStatus.Active: return "На содержании";
                case PrisonContractStatus.Departing: return "Ожидает отправления";
                case PrisonContractStatus.Closed: return "Закрыт";
                case PrisonContractStatus.Failed: return "Провален";
                default: return "Неизвестно";
            }
        }
    }
}
