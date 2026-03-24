import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { EventResponse } from '../../../core/models/models';

@Component({
  selector: 'app-calendar',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div style="padding:32px max(5%,24px);">
      <div class="section-header" style="margin-bottom:24px;">
        <div><h1 style="font-size:1.75rem;">Event Calendar</h1><p style="margin-top:4px;">Browse events month by month</p></div>
      </div>

      <div class="card">
        <div class="card-header" style="display:flex;align-items:center;justify-content:space-between;">
          <button type="button" class="btn btn-ghost btn-icon" (click)="prevMonth()"><span class="material-icons-round">chevron_left</span></button>
          <h2 style="font-size:1.1rem;">{{ viewDate() | date:'MMMM yyyy' }}</h2>
          <button type="button" class="btn btn-ghost btn-icon" (click)="nextMonth()"><span class="material-icons-round">chevron_right</span></button>
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
                style="min-height:80px;border-right:1px solid var(--border);border-bottom:1px solid var(--border);padding:6px;">
                @if (cell.day) {
                  <div [style]="dayNumStyle(cell)" style="font-size:.8rem;font-weight:600;width:26px;height:26px;border-radius:50%;display:flex;align-items:center;justify-content:center;margin-bottom:4px;">
                    {{ cell.day }}
                  </div>
                  @for (ev of cell.events.slice(0,2); track ev.eventId) {
                    <a [routerLink]="['/events', ev.eventId]"
                      style="display:block;font-size:.7rem;padding:2px 5px;border-radius:4px;margin-bottom:2px;overflow:hidden;white-space:nowrap;text-overflow:ellipsis;text-decoration:none;"
                      [style.background]="evColor(ev.category).bg"
                      [style.color]="evColor(ev.category).text">
                      {{ ev.title }}
                    </a>
                  }
                  @if (cell.events.length > 2) {
                    <div style="font-size:.68rem;color:var(--text-muted);padding:1px 4px;">+{{ cell.events.length - 2 }} more</div>
                  }
                }
              </div>
            }
          </div>
        </div>
      </div>

      <!-- Legend -->
      <div style="display:flex;gap:16px;flex-wrap:wrap;margin-top:16px;">
        @for (l of legend; track l.label) {
          <div style="display:flex;align-items:center;gap:6px;font-size:.8rem;color:var(--text-secondary);">
            <div style="width:12px;height:12px;border-radius:3px;" [style.background]="l.bg"></div>{{ l.label }}
          </div>
        }
      </div>
    </div>
  `
})
export class CalendarComponent implements OnInit {
  private eventSvc = inject(EventService);
  events  = signal<EventResponse[]>([]);
  loading = signal(true);
  viewDate = signal(new Date());

  days    = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];
  legend  = [
    { label:'Holiday',   bg:'#FEE2E2' },
    { label:'Awareness', bg:'#D1FAE5' },
    { label:'Public',    bg:'#DBEAFE' },
    { label:'Personal',  bg:'#EDE9FE' },
  ];

  cells = computed(() => {
    const d   = this.viewDate();
    const yr  = d.getFullYear();
    const mo  = d.getMonth();
    const first  = new Date(yr, mo, 1).getDay();
    const daysInMonth = new Date(yr, mo + 1, 0).getDate();
    const today  = new Date();
    const cells: { key: string; day: number|null; isToday: boolean; isOther: boolean; events: EventResponse[] }[] = [];

    for (let i = 0; i < first; i++) cells.push({ key:`p${i}`, day:null, isToday:false, isOther:true, events:[] });
    for (let day = 1; day <= daysInMonth; day++) {
      const date = new Date(yr, mo, day);
      const evs  = this.events().filter(ev => {
        const ed = new Date(ev.eventDate);
        return ed.getFullYear() === yr && ed.getMonth() === mo && ed.getDate() === day;
      });
      const isToday = today.getFullYear()===yr && today.getMonth()===mo && today.getDate()===day;
      cells.push({ key:`${yr}-${mo}-${day}`, day, isToday, isOther:false, events:evs });
    }
    const rem = cells.length % 7;
    if (rem > 0) for (let i = 0; i < 7-rem; i++) cells.push({ key:`n${i}`, day:null, isToday:false, isOther:true, events:[] });
    return cells;
  });

  ngOnInit() {
    this.eventSvc.getAll().subscribe({
      next: evs => { this.events.set(evs); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  prevMonth() { const d = this.viewDate(); this.viewDate.set(new Date(d.getFullYear(), d.getMonth()-1, 1)); }
  nextMonth() { const d = this.viewDate(); this.viewDate.set(new Date(d.getFullYear(), d.getMonth()+1, 1)); }

  cellStyle(c: any): string {
    if (c.isOther) return 'background:var(--surface-2);opacity:.5;';
    if (c.isToday) return 'background:#EEF2FF;';
    return 'background:var(--surface);';
  }
  dayNumStyle(c: any): string {
    return c.isToday ? 'background:var(--primary);color:#fff;' : '';
  }
  evColor(cat: number) {
    const map: Record<number,{bg:string;text:string}> = {
      1:{bg:'#FEE2E2',text:'#991B1B'},
      2:{bg:'#D1FAE5',text:'#065F46'},
      3:{bg:'#DBEAFE',text:'#1E40AF'},
      4:{bg:'#EDE9FE',text:'#5B21B6'},
    };
    return map[cat] ?? {bg:'#F1F5F9',text:'#475569'};
  }
}
