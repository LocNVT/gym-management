import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import localeVi from '@angular/common/locales/vi';
import { KpiCardComponent } from './kpi-card.component';

registerLocaleData(localeVi);

describe('KpiCardComponent', () => {
    let fixture: ComponentFixture<KpiCardComponent>;

    function configure(providers: any[] = []) {
        TestBed.configureTestingModule({
            declarations: [KpiCardComponent],
            imports: [MatIconModule],
            providers,
        });
        fixture = TestBed.createComponent(KpiCardComponent);
        fixture.componentInstance.label = 'Test';
    }

    it('reports the month-over-month change as a percentage', () => {
        configure();
        fixture.componentInstance.value = 12_000_000;
        fixture.componentInstance.previous = 10_000_000;
        fixture.detectChanges();
        expect(fixture.componentInstance.changePercent).toBe(20);
    });

    it('treats growth from zero as no comparison rather than infinity', () => {
        configure();
        fixture.componentInstance.value = 5_000_000;
        fixture.componentInstance.previous = 0;
        fixture.detectChanges();
        expect(fixture.componentInstance.changePercent).toBeNull();
    });

    it('colours a rising cost as bad news, not good, when higherIsBetter is false', () => {
        configure();
        fixture.componentInstance.value = 12_000_000;
        fixture.componentInstance.previous = 10_000_000;
        fixture.componentInstance.higherIsBetter = false;
        fixture.detectChanges();
        // The arrow still follows the actual direction of the number...
        expect(fixture.componentInstance.direction).toBe('up');
        // ...but the colour reflects that a rising cost is bad news.
        expect(fixture.componentInstance.sentiment).toBe('bad');
    });

    it('still colours a rising value as good news by default (higherIsBetter defaults true)', () => {
        configure();
        fixture.componentInstance.value = 12_000_000;
        fixture.componentInstance.previous = 10_000_000;
        fixture.detectChanges();
        expect(fixture.componentInstance.direction).toBe('up');
        expect(fixture.componentInstance.sentiment).toBe('good');
    });

    it('renders currency values Vietnamese-grouped ("." for thousands) rather than American-grouped', () => {
        // Registering the vi locale and forcing LOCALE_ID here mirrors what app.module.ts
        // now does app-wide; this asserts the DecimalPipe actually honours it.
        configure([{ provide: LOCALE_ID, useValue: 'vi-VN' }]);
        fixture.componentInstance.value = 12_000_000;
        fixture.componentInstance.format = 'currency';
        fixture.detectChanges();
        const text = (fixture.nativeElement as HTMLElement).querySelector('.kpi-value')?.textContent ?? '';
        expect(text).toContain('12.000.000');
        expect(text).not.toContain('12,000,000');
    });
});
