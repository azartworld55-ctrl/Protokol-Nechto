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
            get { return new Vector2(720f, 720f); }
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
            Widgets.Label(new Rect(0f, y, inRect.width, 26f), "ИНФРАСТРУКТУРА И БЕЗОПАСНОСТЬ");
            y += 28f;

            PrisonFacilityState facility = state.Facility;
            if (facility == null)
            {
                Widgets.Label(new Rect(0f, y, inRect.width, 24f), "Состояние учреждения не загружено.");
                return;
            }

            Widgets.Label(new Rect(0f, y, inRect.width, 24f),
                "Безопасность: " + facility.GetSecurityRating() + "/100 | Напряжённость: " +
                facility.Tension + "/100 | Питание: " + facility.CachedPowerDemand + "/" +
                facility.CachedPowerCapacity + " | Расходы: " + facility.GetDailyUpkeep() + "/день");
            y += 26f;
            Widgets.Label(new Rect(0f, y, inRect.width, 22f),
                "Режим: " + SecurityModeLabel(facility.CurrentSecurityMode) +
                " | Камеры: " + facility.CountModules(FacilityModuleType.SecurityCamera) +
                " | Охрана: " + facility.CountStaff(PrisonStaffRole.Guard));
            y += 26f;

            if (Widgets.ButtonText(new Rect(0f, y, 210f, 30f), "Сменить режим безопасности"))
                facility.SetSecurityMode((PrisonSecurityMode)(((int)facility.CurrentSecurityMode + 1) % 4));
            y += 36f;

            Widgets.Label(new Rect(0f, y, inRect.width, 22f), "Установить модуль (за внутренние кредиты)");
            y += 24f;
            if (Widgets.ButtonText(new Rect(0f, y, 132f, 30f), "Генератор 220"))
                state.InstallFacilityModule(FacilityModuleType.Generator);
            if (Widgets.ButtonText(new Rect(136f, y, 132f, 30f), "Блок камер 180"))
                state.InstallFacilityModule(FacilityModuleType.CellBlock);
            if (Widgets.ButtonText(new Rect(272f, y, 132f, 30f), "Камера 90"))
                state.InstallFacilityModule(FacilityModuleType.SecurityCamera);
            if (Widgets.ButtonText(new Rect(408f, y, 132f, 30f), "Консоль 160"))
                state.InstallFacilityModule(FacilityModuleType.ControlConsole);
            if (Widgets.ButtonText(new Rect(544f, y, 132f, 30f), "Дверь 120"))
                state.InstallFacilityModule(FacilityModuleType.SecureDoor);
            y += 36f;
            if (Widgets.ButtonText(new Rect(0f, y, 132f, 30f), "Сигнализация 130"))
                state.InstallFacilityModule(FacilityModuleType.AlarmSystem);
            if (Widgets.ButtonText(new Rect(136f, y, 132f, 30f), "Медблок 170"))
                state.InstallFacilityModule(FacilityModuleType.MedicalRoom);
            if (Widgets.ButtonText(new Rect(272f, y, 132f, 30f), "Кухня 150"))
                state.InstallFacilityModule(FacilityModuleType.Kitchen);
            if (Widgets.ButtonText(new Rect(408f, y, 132f, 30f), "Санузел 140"))
                state.InstallFacilityModule(FacilityModuleType.Sanitation);
            if (Widgets.ButtonText(new Rect(544f, y, 132f, 30f), "Досуг 110"))
                state.InstallFacilityModule(FacilityModuleType.RecreationRoom);
            y += 38f;

            Widgets.Label(new Rect(0f, y, inRect.width, 22f),
                "Назначить выбранных пешек персоналом (пешки-заключённые исключены)");
            y += 24f;
            if (Widgets.ButtonText(new Rect(0f, y, 132f, 30f), "Охрана"))
                AssignSelectedStaff(state, PrisonStaffRole.Guard);
            if (Widgets.ButtonText(new Rect(136f, y, 132f, 30f), "Управляющий"))
                AssignSelectedStaff(state, PrisonStaffRole.Warden);
            if (Widgets.ButtonText(new Rect(272f, y, 132f, 30f), "Медик"))
                AssignSelectedStaff(state, PrisonStaffRole.Medic);
            if (Widgets.ButtonText(new Rect(408f, y, 132f, 30f), "Повар"))
                AssignSelectedStaff(state, PrisonStaffRole.Cook);
            if (Widgets.ButtonText(new Rect(544f, y, 132f, 30f), "Техник"))
                AssignSelectedStaff(state, PrisonStaffRole.Technician);
            y += 36f;

            Widgets.Label(new Rect(0f, y, inRect.width, 24f),
                "Модули пока абстрактны: они влияют на питание, расходы и показатели, но не создают здания на карте.");
            y += 24f;
            Widgets.Label(new Rect(0f, y, inRect.width, 24f),
                "Ручной приём пешек — тестовый путь, не прибытие шаттла. Отправление ещё не подключено.");
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

        private static void AssignSelectedStaff(PrisonColonyGameComponent state, PrisonStaffRole role)
        {
            foreach (Pawn pawn in Find.Selector.SelectedPawns)
                state.AssignStaff(pawn, role);
        }

        private static string SecurityModeLabel(PrisonSecurityMode mode)
        {
            switch (mode)
            {
                case PrisonSecurityMode.IncreasedAttention: return "Повышенное внимание";
                case PrisonSecurityMode.Quarantine: return "Карантин";
                case PrisonSecurityMode.Emergency: return "Чрезвычайный";
                default: return "Обычный";
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
