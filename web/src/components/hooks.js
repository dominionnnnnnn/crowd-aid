import { useEffect, useRef, useState } from "react";
export function asError(error) {
  const body = error.response?.data?.error;
  const fields = {};
  for (const [key, value] of Object.entries(body?.fields || {})) {
    const clean = key.replace(/^\$\./, "");
    fields[clean.charAt(0).toLowerCase() + clean.slice(1)] = Array.isArray(
      value,
    )
      ? value.join(" ")
      : String(value);
  }
  return {
    status: error.response?.status,
    code: body?.code,
    message:
      body?.message ||
      (error.code === "ECONNABORTED"
        ? "This is taking longer than usual. Please try again; your answers are kept."
        : error.response
          ? "We could not complete that request. Please try again."
          : error.message ||
            "We could not complete that request. Please try again."),
    fields,
  };
}
export function useResource(loader, deps = []) {
  const [state, setState] = useState({
    data: null,
    error: null,
    loading: true,
  });
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    setState({ data: null, error: null, loading: true });
    Promise.resolve()
      .then(() => loader(controller.signal))
      .then((data) => {
        if (!controller.signal.aborted)
          setState({ data, error: null, loading: false });
      })
      .catch((error) => {
        if (!controller.signal.aborted)
          setState({ data: null, error: asError(error), loading: false });
      });
    return () => controller.abort();
  }, [...deps, revision]);
  return { ...state, reload: () => setRevision((v) => v + 1) };
}
export function useForm(initial, prefix = "") {
  const [values, setValues] = useState(initial);
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState(null);
  const sending = useRef(false);
  const set = (key, value) => {
    setValues((current) => ({ ...current, [key]: value }));
    setError((current) =>
      current
        ? { ...current, fields: { ...current.fields, [key]: undefined } }
        : null,
    );
  };
  const bind = (name) => ({
    name,
    id: `${prefix}${name}`,
    value: values[name] ?? "",
    error: error?.fields?.[name],
    onChange: (e) => set(name, e.target.value),
  });
  async function run(send, validate = () => ({})) {
    if (sending.current) return null;
    const fields = validate(values);
    if (Object.keys(fields).length) {
      setError({ message: "Please fix the highlighted fields.", fields });
      return null;
    }
    sending.current = true;
    setBusy(true);
    setError(null);
    try {
      const response = await send(values);
      setResult(response);
      return response;
    } catch (e) {
      setError(asError(e));
      return null;
    } finally {
      setBusy(false);
      sending.current = false;
    }
  }
  return {
    values,
    setValues,
    set,
    bind,
    error,
    setError,
    busy,
    result,
    setResult,
    run,
    submit: (send, validate) => (e) => {
      e.preventDefault();
      run(send, validate);
    },
    prefix,
  };
}
