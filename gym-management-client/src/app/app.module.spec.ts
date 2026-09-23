import { TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { AppModule } from './app.module';

describe('AppModule', () => {
    it('wires up the vi-VN locale app-wide, not just in a test double', () => {
        TestBed.configureTestingModule({ imports: [AppModule] });
        expect(TestBed.inject(LOCALE_ID)).toBe('vi-VN');
    });
});
