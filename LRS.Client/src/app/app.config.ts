import { ApplicationConfig, provideZoneChangeDetection, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { FormlyModule } from '@ngx-formly/core';
import { FormlyFieldInputComponent } from './shared/components/formly/formly-field-input.component';
import { FormlyFieldSelectComponent } from './shared/components/formly/formly-field-select.component';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    importProvidersFrom(
      FormlyModule.forRoot({
        types: [
          { name: 'input', component: FormlyFieldInputComponent },
          { name: 'select', component: FormlyFieldSelectComponent }
        ],
        validationMessages: [
          { name: 'required', message: 'This field is required.' }
        ]
      })
    )
  ]
};
