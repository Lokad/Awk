# Changelog

## 0.1.0

- Make case-insensitive regex matching independent of the ambient .NET culture, consistently with
  the runtime's C-locale-oriented compatibility policy.
- Preserve record boundaries and `RT` when a running program changes `RS` from a custom separator
  to the default newline separator.
- Accept parenthesized argument lists for `print` and `printf` statements.
- Evaluate regex constants against the current record in scalar expression contexts.
- Document and test that strict UTF-8 ingress is unchanged by C locale settings.
- Raise the default execution-step budget from one million to ten million after calibrating
  representative streaming workloads; explicitly injected tighter limits remain enforced, and
  sufficiently large aggregations can still exhaust the finite default.
- Initial standalone extraction target for Lokad.Awk.
