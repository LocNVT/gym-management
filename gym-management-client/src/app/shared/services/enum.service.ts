import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, shareReplay } from 'rxjs';

export interface EnumValue { value: number; name: string; label: string; }
export type EnumMap = Record<string, EnumValue[]>;

/** DevExtreme lookup shape. */
export interface LookupOption { value: number; text: string; }

@Injectable({ providedIn: 'root' })
export class EnumService {
    // One request per app load; every grid shares the same response.
    private readonly all$: Observable<EnumMap>;

    constructor(private http: HttpClient) {
        this.all$ = this.http
            .get<EnumMap>('/api/Enums')
            .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    }

    options(name: string): Observable<LookupOption[]> {
        return this.all$.pipe(
            map(all => (all[name] ?? []).map(v => ({ value: v.value, text: v.label })))
        );
    }
}
