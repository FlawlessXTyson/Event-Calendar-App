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
  templateUrl: './dashboard-calendar.component.html',
  styleUrls: ['./dashboard-calendar.component.css']
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
