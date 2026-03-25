import { Component, inject, OnInit, signal, computed, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { EventResponse } from '../../../core/models/models';
import { SpecialDay, getSpecialDaysForYear } from '../.././../features/public/calendar/special-days';

interface CalendarDay {
  key: string;
  day: number | null;
  isToday: boolean;
  isOther: boolean;
  events: EventResponse[];
  holidays: SpecialDay[];
  cultural: SpecialDay[];
  awareness: SpecialDay[];
}

@Component({
  selector: 'app-dashboard-calendar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div>
      <div style="margin-bottom:24px;">
        <h1 style="font-size:1.5rem;">Calendar</h1>
        <p>Events, public holidays, cultural celebrations & awareness days</p>
      </div>

      <div class="card">
        <div class="card-header" style="display:flex;align-items:center;justify-content:space-between;padding:12px 16px;">
          <button type="button" class="btn btn-ghost btn-icon" (click)="prevMonth()">
            <span class="material-icons-round">chevron_left</span>
          </button>
          <div style="display:flex;align-items:center;gap:12px;">
            <h2 style="font-size:1.1rem;font-weight:700;">{{ viewDate() | date:'MMMM yyyy' }}</h2>
            <button type="button" class="btn btn-ghost btn-sm" (click)="goToday()" style="font-size:.8rem;">Today</button>
          </div>
          <button type="button" class="btn btn-ghost btn-icon" (click)="nextMonth()">
            <span class="material-icons-round">chevron_right</span>
          </button>
        </div>

        <div class="card-body" style="padding:0;">
          <div style="display:grid;grid-template-columns:repeat(7,1fr);border-bottom:1px solid var(--border);">
            @for (d of days; track d) {
              <div style="padding:10px 0;text-align:center;font-size:.75rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;">{{ d }}</div>
            }
          </div>

          <div style="display:grid;grid-template-columns:repeat(7,1fr);">
            @for (cell of cells(); track cell.key) {
              <div [style]="cellStyle(cell)"
                style="min-height:90px;border-right:1px solid var(--border);border-bottom:1px solid var(--border);padding:6px;overflow:hidden;">
                @if (cell.day) {
                  <div [style]="dayNumStyle(cell)"
                    style="font-size:.8rem;font-weight:600;width:26px;height:26px;border-radius:50%;display:flex;align-items:center;justify-content:center;margin-bottom:3px;">
                    {{ cell.day }}
                  </div>

                  @for (h of cell.holidays.slice(0,1); track h.label) {
                    <div [title]="h.label"
                      style="font-size:.65rem;padding:1px 4px;border-radius:3px;margin-bottom:2px;overflow:hidden;white-space:nowrap;text-overflow:ellipsis;background:#FEE2E2;color:#991B1B;font-weight:600;">
                      🏛 {{ h.label }}
                    </div>
                  }

                  @for (c of cell.cultural.slice(0,1); track c.label) {
                    <div [title]="c.label"
                      style="font-size:.65rem;padding:1px 4px;border-radius:3px;margin-bottom:2px;overflow:hidden;white-space:nowrap;text-overflow:ellipsis;background:#FEF3C7;color:#92400E;font-weight:600;">
                      🎉 {{ c.label }}
                    </div>
                  }

                  @for (a of cell.awareness.slice(0,1); track a.label) {
                    <div [title]="a.label"
                      style="font-size:.65rem;padding:1px 4px;border-radius:3px;margin-bottom:2px;overflow:hidden;white-space:nowrap;text-overflow:ellipsis;background:#D1FAE5;color:#065F46;font-weight:600;">
                      🌍 {{ a.label }}
                    </div>
                  }

                  @for (ev of cell.events.slice(0,2); track ev.eventId) {
                    <a [routerLink]="eventLink(ev)" [title]="ev.title"
                      style="display:block;font-size:.65rem;padding:1px 4px;border-radius:3px;margin-bottom:2px;overflow:hidden;white-space:nowrap;text-overflow:ellipsis;text-decoration:none;"
                      [style.background]="evColor(ev.category).bg"
                      [style.color]="evColor(ev.category).text">
                      📅 {{ ev.title }}
                    </a>
                  }

                  @if ((cell.events.length + cell.holidays.length + cell.cultural.length + cell.awareness.length) > 4) {
                    <div style="font-size:.62rem;color:var(--text-muted);padding:1px 3px;">
                      +{{ (cell.events.length + cell.holidays.length + cell.cultural.length + cell.awareness.length) - 4 }} more
                    </div>
                  }
                }
              </div>
            }
          </div>
        </div>
      </div>

      <!-- Legend -->
      <div style="display:flex;gap:14px;flex-wrap:wrap;margin-top:14px;align-items:center;">
        <span style="font-size:.78rem;color:var(--text-muted);font-weight:600;">Legend:</span>
        @for (l of legend; track l.label) {
          <div style="display:flex;align-items:center;gap:5px;font-size:.78rem;color:var(--text-secondary);">
            <div style="width:11px;height:11px;border-radius:2px;" [style.background]="l.bg"></div>{{ l.label }}
          </div>
        }
      </div>

      <!-- Month list -->
      @if (monthItems().length > 0) {
        <div class="card" style="margin-top:20px;">
          <div class="card-header"><h3 style="font-size:1rem;">{{ viewDate() | date:'MMMM yyyy' }} — All Days</h3></div>
          <div class="card-body" style="padding:0;">
            @for (item of monthItems(); track item.key) {
              <div style="display:flex;align-items:center;gap:12px;padding:10px 16px;border-bottom:1px solid var(--border);">
                <div style="min-width:42px;text-align:center;">
                  <div style="font-size:1.2rem;font-weight:700;line-height:1;">{{ item.day }}</div>
                  <div style="font-size:.7rem;color:var(--text-muted);">{{ item.weekday }}</div>
                </div>
                <div style="flex:1;">
                  <div style="display:flex;align-items:center;gap:6px;flex-wrap:wrap;">
                    <span style="font-size:.82rem;font-weight:600;">{{ item.label }}</span>
                    <span style="font-size:.7rem;padding:1px 7px;border-radius:20px;"
                      [style.background]="item.bg" [style.color]="item.color">{{ item.type }}</span>
                  </div>
                  @if (item.subtitle) {
                    <div style="font-size:.75rem;color:var(--text-muted);margin-top:2px;">{{ item.subtitle }}</div>
                  }
                </div>
                @if (item.link) {
                  <a [routerLink]="item.link" class="btn btn-ghost btn-sm btn-icon">
                    <span class="material-icons-round" style="font-size:16px;">open_in_new</span>
                  </a>
                }
              </div>
            }
          </div>
        </div>
      }
    </div>
  `
})
export class DashboardCalendarComponent implements OnInit {
  @Input() eventRoutePrefix = '/events';

  private eventSvc = inject(EventService);
  events   = signal<EventResponse[]>([]);
  loading  = signal(true);
  viewDate = signal(new Date());

  private specialDaysCache = new Map<number, SpecialDay[]>();

  days   = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];
  legend = [
    { label: 'Public Holiday',       bg: '#FEE2E2' },
    { label: 'Cultural / Religious', bg: '#FEF3C7' },
    { label: 'Awareness Day',        bg: '#D1FAE5' },
    { label: 'Organizer Event',      bg: '#DBEAFE' },
  ];

  private getSpecial(year: number): SpecialDay[] {
    if (!this.specialDaysCache.has(year)) {
      this.specialDaysCache.set(year, getSpecialDaysForYear(year));
    }
    return this.specialDaysCache.get(year)!;
  }

  cells = computed(() => {
    const d   = this.viewDate();
    const yr  = d.getFullYear();
    const mo  = d.getMonth();
    const mo1 = mo + 1;
    const first       = new Date(yr, mo, 1).getDay();
    const daysInMonth = new Date(yr, mo + 1, 0).getDate();
    const today       = new Date();
    const special     = this.getSpecial(yr);
    const result: CalendarDay[] = [];

    for (let i = 0; i < first; i++)
      result.push({ key:`p${i}`, day:null, isToday:false, isOther:true, events:[], holidays:[], cultural:[], awareness:[] });

    for (let day = 1; day <= daysInMonth; day++) {
      const evs      = this.events().filter(ev => {
        const ed = new Date(ev.eventDate);
        return ed.getFullYear()===yr && ed.getMonth()===mo && ed.getDate()===day;
      });
      const daySpecial = special.filter(s => s.month===mo1 && s.day===day);
      const isToday    = today.getFullYear()===yr && today.getMonth()===mo && today.getDate()===day;
      result.push({
        key: `${yr}-${mo}-${day}`, day, isToday, isOther: false, events: evs,
        holidays:  daySpecial.filter(s => s.type==='holiday'),
        cultural:  daySpecial.filter(s => s.type==='cultural'),
        awareness: daySpecial.filter(s => s.type==='awareness'),
      });
    }

    const rem = result.length % 7;
    if (rem > 0)
      for (let i = 0; i < 7-rem; i++)
        result.push({ key:`n${i}`, day:null, isToday:false, isOther:true, events:[], holidays:[], cultural:[], awareness:[] });

    return result;
  });

  monthItems = computed(() => {
    const d   = this.viewDate();
    const yr  = d.getFullYear();
    const mo  = d.getMonth();
    const mo1 = mo + 1;
    const daysInMonth = new Date(yr, mo + 1, 0).getDate();
    const special     = this.getSpecial(yr);
    const weekDayNames = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];
    const items: any[] = [];

    for (let day = 1; day <= daysInMonth; day++) {
      const weekday    = weekDayNames[new Date(yr, mo, day).getDay()];
      const daySpecial = special.filter(s => s.month===mo1 && s.day===day);

      daySpecial.filter(s => s.type==='holiday').forEach(h =>
        items.push({ key:`h-${day}-${h.label}`, day, weekday, label:h.label, type:'Public Holiday', bg:'#FEE2E2', color:'#991B1B', subtitle:'', link:null })
      );
      daySpecial.filter(s => s.type==='cultural').forEach(c =>
        items.push({ key:`c-${day}-${c.label}`, day, weekday, label:c.label, type:'Cultural', bg:'#FEF3C7', color:'#92400E', subtitle:'', link:null })
      );
      daySpecial.filter(s => s.type==='awareness').forEach(a =>
        items.push({ key:`a-${day}-${a.label}`, day, weekday, label:a.label, type:'Awareness Day', bg:'#D1FAE5', color:'#065F46', subtitle:'', link:null })
      );
      this.events().filter(ev => {
        const ed = new Date(ev.eventDate);
        return ed.getFullYear()===yr && ed.getMonth()===mo && ed.getDate()===day;
      }).forEach(ev =>
        items.push({ key:`e-${ev.eventId}`, day, weekday, label:ev.title, type:'Event', bg:this.evColor(ev.category).bg, color:this.evColor(ev.category).text, subtitle:ev.location||'', link:[this.eventRoutePrefix, ev.eventId] })
      );
    }
    return items.sort((a, b) => a.day - b.day);
  });

  ngOnInit() {
    this.eventSvc.getAll().subscribe({
      next: evs => { this.events.set(evs); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  prevMonth() { const d = this.viewDate(); this.viewDate.set(new Date(d.getFullYear(), d.getMonth()-1, 1)); }
  nextMonth() { const d = this.viewDate(); this.viewDate.set(new Date(d.getFullYear(), d.getMonth()+1, 1)); }
  goToday()   { this.viewDate.set(new Date()); }

  cellStyle(c: CalendarDay): string {
    if (c.isOther) return 'background:var(--surface-2);opacity:.4;';
    if (c.isToday) return 'background:#EEF2FF;';
    return 'background:var(--surface);';
  }
  dayNumStyle(c: CalendarDay): string {
    return c.isToday ? 'background:var(--primary);color:#fff;' : '';
  }
  evColor(cat: number) {
    const map: Record<number,{bg:string;text:string}> = {
      1:{bg:'#FEF3C7',text:'#92400E'},
      2:{bg:'#D1FAE5',text:'#065F46'},
      3:{bg:'#DBEAFE',text:'#1E40AF'},
      4:{bg:'#EDE9FE',text:'#5B21B6'},
    };
    return map[cat] ?? {bg:'#F1F5F9',text:'#475569'};
  }
  eventLink(ev: EventResponse) { return [this.eventRoutePrefix, ev.eventId]; }
}
